using DigitalCap.Core.Constants;
using DigitalCap.Core.Helpers.Extensions;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Reporting;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Cors.Infrastructure;
using NPOI.XSSF.Streaming.Values;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DigitalCap.Infrastructure.Service
{
    using TemplateConstants = DigitalCap.Core.Helpers.Constants.Template;
    using Template = DigitalCap.Core.Models.Reporting.Template;
    public class TemplateService : ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly IProjectService _projectService;
        private readonly IProjectRepository _projectRepository;
        private readonly IVesselService _vesselService;
        private readonly IFileStorageRepository _fileStorageRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskService _taskService;
        private readonly IPortRepository _portRepository;
        private class SectionDocument
        {
            public TemplateSection Section { get; set; }
            public byte[] Data { get; set; }
        }
        public TemplateService(ITemplateRepository templateRepository, IProjectService projectService,
            IVesselService vesselService, IProjectRepository projectRepository,
            IFileStorageRepository fileStorageRepository, ITaskService taskService, ITaskRepository taskRepository, IPortRepository portRepository)
        {
            _templateRepository = templateRepository;
            _projectService = projectService;
            _vesselService = vesselService;
            _projectRepository = projectRepository;
            _fileStorageRepository = fileStorageRepository;
            _taskService = taskService;
            _taskRepository = taskRepository;
            _portRepository = portRepository;
        }

        private static void ReplaceTokens(WordprocessingDocument document, IDictionary<string, Func<string>> replacements)
        {
            var mainDocument = document.MainDocumentPart;
            var headerNodes = mainDocument.HeaderParts.Select(x => x.RootElement);
            var footerNodes = mainDocument.FooterParts.Select(x => x.RootElement);
            var bodyNode = mainDocument.Document.Body;
            var allNodes = headerNodes.Concat(footerNodes).Concat(bodyNode);

            foreach (var currentNode in allNodes)
            {
                var contentRunElements = currentNode.Descendants<SdtContentRun>();
                ReplaceTokens(contentRunElements, replacements);

                var paragraphElements = currentNode.Descendants<Paragraph>();
                ReplaceTokens(paragraphElements, replacements);

                var textElements = currentNode.Descendants<Text>();
                ReplaceTokens(textElements, replacements);
            }
        }

        private static void ReplaceTokens(IEnumerable<OpenXmlElement> source, IDictionary<string, Func<string>> replacements)
        {
            var toRemove = new List<OpenXmlElement>();

            foreach (var currentElement in source)
            {
                var content = currentElement.InnerText;

                if (!replacements.Keys.Any(x => content.Contains(TemplateConstants.Tokenize(x), StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                var runs = currentElement.Elements<Run>();

                ReplaceTokens(runs, toRemove, replacements);
            }

            toRemove.ForEach(x => x.Remove());
        }

        private static void ReplaceTokens(IEnumerable<Run> source, List<OpenXmlElement> toRemove, IDictionary<string, Func<string>> replacements)
        {
            foreach (var currentRun in source)
            {
                var runContent = currentRun.InnerText;

                if (runContent.Equals(TemplateConstants.TokenPrefix) || runContent.Equals(TemplateConstants.TokenSuffix))
                {
                    toRemove.Add(currentRun);
                }
                else if (replacements.ContainsKey(runContent))
                {
                    var textElements = currentRun.Elements<Text>();
                    ReplaceTokens(textElements, replacements);
                }
                else
                {
                    if (!replacements.ContainsKey(runContent))
                        continue;

                    var textElement = currentRun.Elements<Text>().SingleOrDefault();

                    textElement.Text = replacements[runContent]() ?? TemplateConstants.TokenizeManualEntry(runContent);
                }
            }
        }

        private static void ReplaceTokens(IEnumerable<Text> source, IDictionary<string, Func<string>> replacements)
        {
            foreach (var currentElement in source)
            {
                var content = currentElement.Text;
                var doesRequireManualEntry = false;

                foreach (var currentPair in replacements)
                {
                    if (string.IsNullOrWhiteSpace(content))
                        break;

                    var key = TemplateConstants.Tokenize(currentPair.Key);

                    if (!content.Contains(key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        key = currentPair.Key;

                        if (!content.Equals(key))
                            continue;
                    }

                    var replacementValue = currentPair.Value();

                    if (replacementValue == null)
                    {
                        doesRequireManualEntry = true;
                        replacementValue = TemplateConstants.TokenizeManualEntry(currentPair.Key);
                    }

                    content = Regex.Replace(content, key, replacementValue, RegexOptions.IgnoreCase);
                }

                currentElement.Text = content;

                if (doesRequireManualEntry
                 && currentElement.Parent is Run parentRun)
                {
                    var properties = parentRun.GetFirstChild<RunProperties>();

                    if (properties == null)
                    {
                        properties = new RunProperties();
                        parentRun.Append(properties);
                    }

                    if (properties.Color == null)
                        properties.Color = new Color();

                    properties.Color.Val = "FF0000";

                    // The text must be removed and added back to the 
                    // run in order to get the run property changes
                    // to consistently apply.
                    var clonedText = currentElement.CloneNode(true);
                    currentElement.Remove();
                    parentRun.Append(clonedText);
                }
            }
        }
    }
}
