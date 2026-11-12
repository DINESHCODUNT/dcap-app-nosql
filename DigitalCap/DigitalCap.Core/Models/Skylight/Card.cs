
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Models.Skylight
{
    public class Component
    {
        [Required(AllowEmptyStrings = false)]
        public virtual ComponentType ComponentType { get; set; }
        public Done Done { get; set; }
        public string SequenceId { get; set; }
        public Dictionary<string, Choice> Choices { get; set; }
        public ComponentDecisionLink Link { get; set; }
        public Action DoneAction { get; set; }
        public bool? IncludeCapture { get; set; }
        public int? MaxSelected { get; set; }
        public List<string> Captures { get; set; } = new List<string>();
        public List<ComponentDecisionCaptures> DecisionCaptures { get; set; }
        public int? MinSelected { get; set; }
        public bool? Mutable { get; set; }
        public int? Delay { get; set; }


    }
    public enum ComponentType
    {
        Default = 0,
        Calling = 1,
        CapturePhoto = 2,
        CaptureVideo = 3,
        CaptureAudio = 4,
        Completion = 5,
        Scanning = 6,
        Decision = 7,
        OpenSequence = 8
    }
    public class ComponentDefault : Component
    {

        [Required(AllowEmptyStrings = false)]
        public override ComponentType ComponentType { get; set; }
        public Done Done { get; set; }
        public Action DoneAction { get; set; }
    }
    public class ComponentCompletion : Component
    {

        [Required(AllowEmptyStrings = false)]
        public override ComponentType ComponentType { get; set; }
        public bool? Completed { get; set; }
        public string CompletionTime { get; set; }
        public Done Done { get; set; }
        public Action DoneAction { get; set; }
        public int? MoveDelay { get; set; }
        public bool? ReturnPrev { get; set; }
        public bool? ScrollLock { get; set; }
    }

    public class Layout
    {

        [Required(AllowEmptyStrings = false)]
        public virtual LayoutType LayoutType { get; set; }

        public LayoutTextInputFormat Format { get; set; }
        public string HelpText { get; set; }
        public string Input { get; set; }
        public bool? Mutable { get; set; }
        public string Placeholder { get; set; }
        public bool? ShowIcon { get; set; }

        public Alignment Alignment { get; set; }
        public bool? AutoFit { get; set; }
        public string BgColor { get; set; }
        public string FgColor { get; set; }
        [Required(AllowEmptyStrings = false)]
        public string Text { get; set; }
        [Required(AllowEmptyStrings = false)]
        public TextSize TextSize { get; set; }

    }

    public enum LayoutType
    {
        Text = 0,
        Web = 1,
        TextInput = 2,
        Image = 3,
        AvMedia = 4,
        Pdf = 5,
        Svg = 6,
        Dynamic = 7
    }
    public enum LayoutTextInputFormatType
    {
        Generic = 0,
        Number = 1
    }
    public class LayoutTextInputFormat
    {
        public LayoutTextInputFormatType Type { get; set; }

        public int? MaxChar { get; set; }

    }
    public class LayoutTextInput : Layout
    {
        public override LayoutType LayoutType { get; set; }
        public LayoutTextInputFormat Format { get; set; }
        public string HelpText { get; set; }
        public string Input { get; set; }
        public bool? Mutable { get; set; }
        public string Placeholder { get; set; }
        public bool? ShowIcon { get; set; }
    }

    public enum Alignment
    {
        Center = 0,
        Left = 1,
        Right = 2
    }
    public enum TextSize
    {
        Small = 0,
        Large = 1
    }
    public class LayoutText : Layout
    {
        [Required(AllowEmptyStrings = false)]
        public override LayoutType LayoutType { get; set; }
        public Alignment Alignment { get; set; }
        public bool? AutoFit { get; set; }
        public string BgColor { get; set; }
        public string FgColor { get; set; }
        [Required(AllowEmptyStrings = false)]
        public string Text { get; set; }
        [Required(AllowEmptyStrings = false)]
        public TextSize TextSize { get; set; }

    }
    public enum DoneType
    {
        CallConnected = 0,
        OnFocus = 1,
        OnSelect = 2,
        DataCommitted = 3,
        MediaWatched = 4,
        MinCaptured = 5,
        MinChoices = 6,
        ScanSuccess = 7
    }
    public class Done
    {
        public virtual DoneType Type { get; set; }
    }
    public class ComponentCapturePhoto : Component
    {
        public override ComponentType ComponentType { get; set; }
        public List<string> Captures { get; set; }
        public int? Delay { get; set; }
        public Done Done { get; set; }
        public Action DoneAction { get; set; }
    }
    public class ComponentDecisionLink
    {
        [Required(AllowEmptyStrings = false)]
        public string Text { get; set; }
        [Required(AllowEmptyStrings = false)]
        public string SequenceId { get; set; }
        public string CardId { get; set; }
    }
    public class Choice
    {
        public string Label { get; set; }
        public decimal? Position { get; set; }
        public bool? Selected { get; set; }
        public Action Action { get; set; }
    }

    public class ComponentDecision : Component
    {
        public override ComponentType ComponentType { get; set; }
        public Dictionary<string, Choice> Choices { get; set; }
        public ComponentDecisionLink Link { get; set; }
        public Done Done { get; set; }
        public Action DoneAction { get; set; }
        public bool? IncludeCapture { get; set; }
        public int? MaxSelected { get; set; }
        public List<ComponentDecisionCaptures> ComponentDecisionCaptures { get; set; }
        public int? MinSelected { get; set; }
        public bool? Mutable { get; set; }


    }
    public class ComponentDecisionCaptures
    {
        public string Id { get; set; }
        public string MimeType { get; set; }
    }

    public class Card
    {
        public string Label { get; set; }
        public string Updated { get; set; }
        public string TemplateId { get; set; }
        public List<string> Tags { get; set; }
        public bool? Subdued { get; set; }
        public int? Size { get; set; }
        public string SequenceId { get; set; }
        public bool? Selectable { get; set; }
        public int? Revision { get; set; }
        public bool? Required { get; set; }
        public decimal? Position { get; set; }
        public List<Guid?> Notes { get; set; }
        public bool? Locked { get; set; }
        public Layout Layout { get; set; }
        public List<string> Voice { get; set; }
        public bool? IsDone { get; set; }
        public string IntegrationId { get; set; }
        public string Id { get; set; }
        public bool? HideLabel { get; set; }
        public string Header { get; set; }
        public string Footer { get; set; }
        public string CreatedBy { get; set; }
        public string Created { get; set; }
        public Component Component { get; set; }
        public string AssignmentId { get; set; }
        public string UpdatedBy { get; set; }
    }
}