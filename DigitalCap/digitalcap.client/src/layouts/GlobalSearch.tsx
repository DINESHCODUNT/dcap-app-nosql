import React from 'react';
import { SearchInput } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import * as projectService from '../service/ProjectService';
import { useNavigate } from 'react-router-dom';

interface ProjectListItem {
    id: string;
    project_name: string;
    imo_number: string;
    client_name?: string;
}

const GlobalSearch: React.FC = () => {
    const navigate = useNavigate();
    const [open, setOpen] = React.useState(false);
    const [query, setQuery] = React.useState('');
    const [projects, setProjects] = React.useState<ProjectListItem[]>([]);
    const [loading, setLoading] = React.useState(false);
    const containerRef = React.useRef<HTMLDivElement>(null);

    React.useEffect(() => {
        const handleClickOutside = (e: MouseEvent) => {
            if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
                setOpen(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const loadData = async () => {
        if (projects.length > 0 || loading) return;
        setLoading(true);
        try {
            const res = await projectService.getAllProjectsGridData();
            const items = projectService.normalizeAllProjectsGridPayload(res.data);
            const mapped: ProjectListItem[] = items.map((p: any) => ({
                id: String(p.id),
                project_name: p.name || 'Unnamed Project',
                imo_number: p.imo || 'N/A',
                client_name: p.companyName || ''
            }));
            setProjects(mapped);
        } catch (e) {
        } finally {
            setLoading(false);
        }
    };

    const handleOpenSearch = () => {
        setOpen(true);
        void loadData();
    };

    const q = query.trim().toLowerCase();
    const results = projects.filter(
        (p) => !q || p.project_name.toLowerCase().includes(q) || p.imo_number.toLowerCase().includes(q)
    );

    const handleSelect = (project: ProjectListItem) => {
        setQuery('');
        setOpen(false);
        navigate('/app/Project', { state: { projectId: project.id, projectName: project.project_name, showOverview: false } });
    };

    return (
        <div className="global-search ms-auto d-none d-lg-block" ref={containerRef}>
            <div className="global-search-input" onClick={handleOpenSearch}>
                <SearchInput
                    circular
                    value={query}
                    onChange={(e: React.ChangeEvent<HTMLInputElement> | string) => {
                        const value = typeof e === 'string' ? e : e.target.value;
                        setQuery(value);
                        handleOpenSearch();
                    }}
                    onClearSearch={() => {
                        setQuery('');
                        handleOpenSearch();
                    }}
                    placeholder="Search by Project Name or IMO"
                />
            </div>

            {open && (
                <div className="global-search-dropdown shadow mt-1 pt-2 pb-2">
                    <div className="global-search-body">
                        <h6 className="global-search-heading px-3 pb-2 mb-2 fw-bold">
                            Search Results
                            <button
                                type="button"
                                className="global-search-close btn btn-link p-0 text-dark"
                                aria-label="Close search results"
                                onClick={() => setOpen(false)}
                            >
                                <Icon.X size={16} />
                            </button>
                        </h6>
                        <ul className="global-search-list m-0 p-0">
                            {loading && projects.length === 0 && (
                                <li className="global-search-empty text-center py-2">Loading projects...</li>
                            )}
                            {!loading && results.length === 0 && (
                                <li className="global-search-empty text-center py-2">No results found</li>
                            )}
                            {results.map((p) => (
                                <li
                                    key={p.id}
                                    className="global-search-item"
                                    role="button"
                                    tabIndex={0}
                                    onClick={() => handleSelect(p)}
                                    onKeyDown={(e) => e.key === 'Enter' && handleSelect(p)}
                                >
                                    <div className="global-search-trail">
                                        <p className="trail-part trail-last mb-0">{p.project_name}</p>
                                        <p className="trail-imo mb-0 text-muted small">IMO {p.imo_number} <span className="text-divider"><Icon.CircleFill size={6} /> </span>{p.client_name}</p>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>
                </div>
            )}
        </div>
    );
};

export default GlobalSearch;
