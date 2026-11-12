import React, { useState, useMemo, useEffect } from 'react';
import { AbsSearch } from 'customer_portal-ui-shared';
import * as Icon from 'react-bootstrap-icons';

interface ManageTankRow {
    vesselType: string;
    tankType: string;
    status: string;
    tankName: string;
    subheader: string;
}

interface TreeNode {
    id: string;
    name: string;
    type: 'vesseltype' | 'tanktype';
    children?: TreeNode[];
    vesselType?: string;
    tankType?: string;
}

interface ManageTankTemplateListProps {
    data?: ManageTankRow[];
    onFilterChange?: (filters: { vesselType?: string; tankType?: string }) => void;
    selectedFilters?: { vesselType?: string; tankType?: string };
}

const ManageTankTemplateList: React.FC<ManageTankTemplateListProps> = ({
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

        const vesselMap = new Map<string, Set<string>>();

        data.forEach(item => {
            const vesselType = item.vesselType || '';
            const tankType = item.tankType || '';

            if (!vesselMap.has(vesselType)) {
                vesselMap.set(vesselType, new Set());
            }

            vesselMap.get(vesselType)!.add(tankType);
        });

        const tree: TreeNode[] = [];
        let vesselIdCounter = 0;
        let tankIdCounter = 0;

        vesselMap.forEach((tankSet, vesselType) => {
            const vesselId = `v${++vesselIdCounter}`;
            const vesselNode: TreeNode = {
                id: vesselId,
                name: vesselType,
                type: 'vesseltype',
                vesselType: vesselType,
                children: []
            };

            tankSet.forEach(tankType => {
                const tankId = `t${++tankIdCounter}`;
                vesselNode.children!.push({
                    id: tankId,
                    name: tankType,
                    type: 'tanktype',
                    vesselType: vesselType,
                    tankType: tankType
                });
            });

            tree.push(vesselNode);
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
            if (node.type === 'vesseltype') {
                onFilterChange({
                    vesselType: node.vesselType,
                    tankType: undefined
                });
            } else if (node.type === 'tanktype') {
                onFilterChange({
                    vesselType: node.vesselType,
                    tankType: node.tankType
                });
            }
        }

        if (node.children && node.children.length > 0) {
            toggleNode(node.id, e);
        }
    };

    const isNodeSelected = (node: TreeNode): boolean => {
        if (node.type === 'vesseltype') {
            return selectedFilters.vesselType === node.vesselType &&
                !selectedFilters.tankType;
        } else if (node.type === 'tanktype') {
            if (selectedFilters.tankType !== node.tankType) {
                return false;
            }

            if (!selectedFilters.vesselType) {
                return true;
            }

            return selectedFilters.vesselType === node.vesselType;
        }
        return false;
    };

    useEffect(() => {
        if (!selectedFilters.tankType && !selectedFilters.vesselType) {
            return;
        }

        setExpandedNodes(prev => {
            const next = new Set(prev);
            treeData.forEach(vesselNode => {
                const matchesVessel =
                    selectedFilters.vesselType &&
                    vesselNode.vesselType === selectedFilters.vesselType;
                const containsTankType = selectedFilters.tankType &&
                    vesselNode.children?.some(child => child.tankType === selectedFilters.tankType);

                if (matchesVessel || containsTankType) {
                    next.add(vesselNode.id);
                }
            });
            return next;
        });
    }, [selectedFilters.vesselType, selectedFilters.tankType, treeData]);

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
                            <Icon.LightningCharge className="me-2" size={16} />
                        )}
                        <span className={`tree-label ${node.type === 'vesseltype' ? 'tree-label-template fw-bold' : ''} ${node.type === 'tanktype' ? 'tree-label-section' : ''}`}>
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
                            placeholder="Search by Vessel Type or Tank Type"
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

export default ManageTankTemplateList;