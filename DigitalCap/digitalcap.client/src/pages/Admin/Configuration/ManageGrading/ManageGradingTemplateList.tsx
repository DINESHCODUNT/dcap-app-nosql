import React, { useState, useMemo } from 'react';
import { AbsSearch } from 'customer_portal-ui-shared';
import * as Icon from 'react-bootstrap-icons';

interface ManageGradingRow {
    gradingId: number;
    sectionId: string;
    tanktypeId: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    gradingName: string;
    isActive: boolean;
    requiredInReport: boolean;
}

interface TreeNode {
    id: string;
    name: string;
    type: 'template' | 'part' | 'section';
    children?: TreeNode[];
    vesselType?: string;
    templateName?: string;
    sectionName?: string;
}

interface ManageGradingTemplateListProps {
    data?: ManageGradingRow[];
    onFilterChange?: (filters: { vesselType?: string; templateName?: string; sectionName?: string }) => void;
    selectedFilters?: { vesselType?: string; templateName?: string; sectionName?: string };
}

const ManageGradingTemplateList: React.FC<ManageGradingTemplateListProps> = ({
    data = [],
    onFilterChange,
    selectedFilters = {}
}) => {
    const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());
    const [val, updateVal] = React.useState<string>("");
    const [searchApplied, updateSearchApplied] = React.useState<string | null>(null);

    const nodeMatchesSearch = (node: TreeNode, searchText: string): boolean => {
        if (!searchText) return true;
        const searchLower = searchText.toLowerCase();
        return node.name.toLowerCase().includes(searchLower);
    };

    const filterTree = (nodes: TreeNode[], searchText: string): TreeNode[] => {
        if (!searchText) return nodes;

        const filtered: TreeNode[] = [];

        nodes.forEach(node => {
            const nodeMatches = nodeMatchesSearch(node, searchText);
            let filteredChildren: TreeNode[] = [];

            if (node.children) {
                filteredChildren = filterTree(node.children, searchText);
            }

            if (nodeMatches || filteredChildren.length > 0) {
                const filteredNode: TreeNode = {
                    ...node,
                    children: filteredChildren.length > 0 ? filteredChildren : node.children
                };
                filtered.push(filteredNode);
            }
        });

        return filtered;
    };

    const treeData: TreeNode[] = useMemo(() => {
        if (!data || data.length === 0) return [];

        const vesselMap = new Map<string, Map<string, Set<string>>>();

        data.forEach(item => {
            const vesselType = item.vesselType || '';
            const templateName = item.templateName || '';
            const sectionName = item.sectionName || '';

            if (!vesselMap.has(vesselType)) {
                vesselMap.set(vesselType, new Map());
            }

            const templateMap = vesselMap.get(vesselType)!;
            if (!templateMap.has(templateName)) {
                templateMap.set(templateName, new Set());
            }

            templateMap.get(templateName)!.add(sectionName);
        });

        const tree: TreeNode[] = [];
        let templateIdCounter = 0;
        let partIdCounter = 0;
        let sectionIdCounter = 0;

        vesselMap.forEach((templateMap, vesselType) => {
            const templateId = `t${++templateIdCounter}`;
            const templateNode: TreeNode = {
                id: templateId,
                name: vesselType,
                type: 'template',
                vesselType: vesselType,
                children: []
            };

            templateMap.forEach((sectionSet, templateName) => {
                const partId = `p${++partIdCounter}`;
                const partNode: TreeNode = {
                    id: partId,
                    name: templateName,
                    type: 'part',
                    vesselType: vesselType,
                    templateName: templateName,
                    children: []
                };

                sectionSet.forEach(sectionName => {
                    const sectionId = `s${++sectionIdCounter}`;
                    partNode.children!.push({
                        id: sectionId,
                        name: sectionName,
                        type: 'section',
                        vesselType: vesselType,
                        templateName: templateName,
                        sectionName: sectionName
                    });
                });

                templateNode.children!.push(partNode);
            });

            tree.push(templateNode);
        });

        const searchText = val.trim();
        const filteredTree = searchText ? filterTree(tree, searchText) : tree;

        if (searchText && filteredTree.length > 0) {
            const nodesToExpand = new Set<string>();
            const expandMatchingNodes = (nodes: TreeNode[]) => {
                nodes.forEach(node => {
                    if (nodeMatchesSearch(node, searchText) || (node.children && node.children.length > 0)) {
                        nodesToExpand.add(node.id);
                        if (node.children) {
                            expandMatchingNodes(node.children);
                        }
                    }
                });
            };
            expandMatchingNodes(filteredTree);
            setExpandedNodes(nodesToExpand);
        } else if (filteredTree.length > 0) {
            setExpandedNodes(prev => {
                const newSet = new Set(prev);
                filteredTree.forEach(node => newSet.add(node.id));
                return newSet;
            });
        }

        return filteredTree;
    }, [data, val]);

    const toggleNode = (nodeId: string, e?: React.MouseEvent) => {
        e?.stopPropagation();
        const newExpanded = new Set(expandedNodes);
        if (newExpanded.has(nodeId)) {
            newExpanded.delete(nodeId);
        } else {
            newExpanded.add(nodeId);
        }
        setExpandedNodes(newExpanded);
    };

    const handleNodeClick = (node: TreeNode, e: React.MouseEvent) => {
        e.stopPropagation();

        if (onFilterChange) {
            if (node.type === 'template') {
                onFilterChange({
                    vesselType: node.vesselType,
                    templateName: undefined,
                    sectionName: undefined
                });
            } else if (node.type === 'part') {
                onFilterChange({
                    vesselType: node.vesselType,
                    templateName: node.templateName,
                    sectionName: undefined
                });
            } else if (node.type === 'section') {
                onFilterChange({
                    vesselType: node.vesselType,
                    templateName: node.templateName,
                    sectionName: node.sectionName
                });
            }
        }

        if (node.children && node.children.length > 0) {
            toggleNode(node.id, e);
        }
    };

    const isNodeSelected = (node: TreeNode): boolean => {
        if (node.type === 'template') {
            return selectedFilters.vesselType === node.vesselType &&
                !selectedFilters.templateName &&
                !selectedFilters.sectionName;
        } else if (node.type === 'part') {
            return selectedFilters.vesselType === node.vesselType &&
                selectedFilters.templateName === node.templateName &&
                !selectedFilters.sectionName;
        } else if (node.type === 'section') {
            return selectedFilters.vesselType === node.vesselType &&
                selectedFilters.templateName === node.templateName &&
                selectedFilters.sectionName === node.sectionName;
        }
        return false;
    };

    const renderTree = (node: TreeNode, level: number = 0): React.ReactNode => {
        const isExpanded = expandedNodes.has(node.id);
        const hasChildren = node.children && node.children.length > 0;
        const paddingLeft = level * 20;
        const isSelected = isNodeSelected(node);

        return (
            <div key={node.id}>
                <div
                    className={`tree-node d-flex align-items-center py-1 ${hasChildren ? 'tree-node-clickable' : ''} ${isSelected ? 'tree-node-selected' : ''}`}
                    style={{
                        paddingLeft: `${paddingLeft}px`,
                        cursor: 'pointer',
                        backgroundColor: isSelected ? '#e3f2fd' : 'transparent'
                    }}
                    onClick={(e) => handleNodeClick(node, e)}
                >
                    {hasChildren && (
                        <span className="toggle-icon me-2" onClick={(e) => { e.stopPropagation(); toggleNode(node.id, e); }}>
                            {isExpanded ? '−' : '+'}
                        </span>
                    )}
                    <div className="d-flex align-items-center flex-grow-1">
                        {!hasChildren && (
                            <Icon.FileText className="me-2" size={16} />
                        )}
                        <span className={`tree-label ${node.type === 'template' ? 'tree-label-template fw-bold' : ''} ${node.type === 'section' ? 'tree-label-section' : ''}`}>
                            {node.type === 'template'}
                            {node.type === 'part'}
                            {node.type === 'section'}
                            {node.name}
                        </span>
                    </div>
                </div>
                {isExpanded && node.children && (
                    <div className={level === 0 || level === 1 ? 'tree-children-connector' : ''}>
                        {node.children.map(child => renderTree(child, level + 1))}
                    </div>
                )}
            </div>
        );
    };

    return (
        <div className="container-fluid p-20 tree-container bg-light"  >
            <div className="bg-white rounded shadow-sm tree-card">
                <div className="p-3 border-bottom bg-white rounded-top d-flex gap-2 align-items-center">
                    <div className="position-relative flex-grow-1 me-2">
                        <AbsSearch
                            handleClear={() => {
                                updateVal("");
                                updateSearchApplied(null);
                            }}
                            value={val}
                            handleChange={(e: any) => updateVal(e.target.value)}
                            handleSearch={() => {
                                updateSearchApplied(val === "" ? null : val);
                            }}
                            placeholder="Search by Template, Part or Section"
                            searchApplied={searchApplied !== null}
                        />


                    </div>
                </div>
                <div className="p-3 tree-scroll-area" style={{ maxHeight: '600px', overflowY: 'auto' }}>
                    {treeData.length > 0 ? (
                        treeData.map(node => renderTree(node))
                    ) : (
                        <div className="text-center text-muted py-4">No data available</div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default ManageGradingTemplateList;