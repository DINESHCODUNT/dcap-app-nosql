import {
    useCallback,
    useEffect,
    useImperativeHandle,
    useMemo,
    useRef,
    useState,
    forwardRef,
} from "react";
import * as Icon from "react-bootstrap-icons";
import { Divider, TextArea, Button } from "customer_portal-ui-shared";
import moment from "moment";
import * as commentService from "../../service/CommentService";
import type { Comment } from "../../service/CommentService";
import { normalizeCommentFromApi } from "../../service/CommentService";
import { usePopup } from "../../Common/PopupProvider";
import { resultMessage } from "../../Common/UtilityMessage";
import AppLoader from "../../Common/AppLoader";
const COMMENT_MAX_LENGTH = 4000;

export interface WorkflowCommentItem {
    id: number;
    initials: string;
    email: string;
    time: string;
    text: string;
    editable?: boolean;
}

function initialsFromUser(createdBy: string): string {
    const s = (createdBy || "").trim();
    if (!s) return "?";
    const local = s.includes("@") ? s.split("@")[0] : s;
    const parts = local.split(/[.\s_-]+/).filter(Boolean);
    if (parts.length >= 2) {
        return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return local.slice(0, 2).toUpperCase();
}

export type CommentsProps = {
    projectId: number;
    globalpermission: any;
    projectTaskId?: number;
    notes?: string;
    sectionTitleClassName?: string;
    activeTab?: number;
    readOnly?: boolean;
    refreshTrigger?: number;
    onCommentChanged?: () => void;
};

export type CommentsSaveOptions = {
    suppressSuccessPopup?: boolean;
};

export type CommentsHandle = {
    saveIfNeeded: (opts?: CommentsSaveOptions) => Promise<boolean>;
};

const Comments = forwardRef<CommentsHandle, CommentsProps>(({
    projectId,
    projectTaskId,
    globalpermission,
    notes,
    sectionTitleClassName = "",
    activeTab,
    readOnly,
    refreshTrigger,
    onCommentChanged
}, ref) => {
    const taskIdForApi = projectTaskId ?? 0;

    const [comments, setComments] = useState<Comment[]>([]);
    const [newComment, setNewComment] = useState("");
    const [editingCommentId, setEditingCommentId] = useState<number | null>(null);
    const [postingComment, setPostingComment] = useState(false);
    const popup = usePopup();
    const [commentError, setCommentError] = useState("");
    const [deletingCommentId, setDeletingCommentId] = useState<number | null>(null);
    const [commentsLoading, setCommentsLoading] = useState<boolean>(true);
    const loadedOnceRef = useRef<boolean>(false);

    useEffect(() => {
        loadedOnceRef.current = false;
    }, [projectId, projectTaskId]);

    useEffect(() => {
        setEditingCommentId(null);
        setNewComment('');
        setCommentError('');
    }, [activeTab]);
    const canUseComments = projectId > 0 && Number.isFinite(taskIdForApi) && !Number.isNaN(taskIdForApi);

    const loadComments = useCallback(async () => {
        if (!canUseComments) {
            setComments([]);
            setCommentsLoading(false);
            return;
        }
        if (!loadedOnceRef.current) {
            setCommentsLoading(true);
        }
        try {
            const response = await commentService.getComments(projectId, taskIdForApi, "");
            const raw = response.data && Array.isArray(response.data) ? response.data : [];
            setComments(raw.map(normalizeCommentFromApi));
        } catch {
            setComments([]);
        } finally {
            loadedOnceRef.current = true;
            setCommentsLoading(false);
        }
    }, [canUseComments, projectId, taskIdForApi, notes]);

    const handleDeleteComment = async (c: Comment) => {
        const confirmed = await popup.confirm({
            title: "Delete Comment",
            message: "Are you sure you want to delete this comment?",
            confirmText: "Delete",
            danger: true
        });
        if (!confirmed) return;

        setDeletingCommentId(c.id);
        try {
            await commentService.deleteComment(
                c.id,
                c.projectId ?? projectId,
                c.comment ?? ""
            );
            await loadComments();
            onCommentChanged?.();
            popup.success({
                title: "Delete Comment",
                message: resultMessage("Comment", "Deleted")
            });
        } catch (e: any) {
            setCommentError("Could not delete comment. Try again.");
            popup.error({ message: e?.message || "Could not delete comment. Try again." });
        } finally {
            setDeletingCommentId(null);
        }
    };

    useEffect(() => {
        void loadComments();
    }, [loadComments, refreshTrigger]);

    const sortedComments = useMemo(
        () =>
            [...comments].sort(
                (a, b) =>
                    new Date(b.lastModifiedDate).getTime() - new Date(a.lastModifiedDate).getTime()
            ),
        [comments]
    );

    const items: WorkflowCommentItem[] = useMemo(
        () =>
            sortedComments.map((c) => ({
                id: c.id,
                initials: initialsFromUser(c.createdBy),
                email: c.createdBy,
                time: moment(c.lastModifiedDate).format("DD-MMM-YYYY hh:mm A"),
                text: c.comment ?? "",
                editable: c.editable !== false,
            })),
        [sortedComments]
    );

    const hideListWhenEmpty = true;
    const list = items;
    const showList = !(hideListWhenEmpty && list.length === 0);

    const handleStartEditComment = (c: Comment) => {
        setEditingCommentId(c.id);
        setNewComment(c.comment ?? "");
        setCommentError("");
    };

    const persistComment = async (opts?: CommentsSaveOptions): Promise<boolean> => {
        const text = newComment.trim();
        if (!text || !canUseComments) return true;
        if (text.length > COMMENT_MAX_LENGTH) {
            setCommentError(`Comment cannot exceed ${COMMENT_MAX_LENGTH} characters`);
            return false;
        }
        setPostingComment(true);
        setCommentError("");
        const wasEditing = editingCommentId != null;
        try {
            if (editingCommentId != null) {
                const existing = comments.find((c) => c.id === editingCommentId);
                if (!existing) {
                    setCommentError("This comment is no longer available. Reload the list and try again.");
                    setEditingCommentId(null);
                    return false;
                }
                if ((existing.comment ?? "").trim() === text) {
                    setNewComment("");
                    setEditingCommentId(null);
                    return true;
                }
                await commentService.updateComment(
                    existing.id,
                    existing.projectId ?? projectId,
                    text
                );
            } else {
                await commentService.createComment(projectId, 0, text);
            }
            setNewComment("");
            setEditingCommentId(null);
            await loadComments();
            onCommentChanged?.();
            if (!opts?.suppressSuccessPopup) {
                popup.success({
                    title: wasEditing ? "Edit Comment" : "Create Comment",
                    message: resultMessage("Comment", wasEditing ? "Updated" : "Saved")
                });
            }
            return true;
        } catch (e) {
            setCommentError("Could not save comment. Try again.");
            return false;
        } finally {
            setPostingComment(false);
        }
    };

    useImperativeHandle(ref, () => ({
        saveIfNeeded: (opts) => persistComment(opts),
    }));

    const handlePostOrUpdateComment = async () => {
        await persistComment();
    };

    const submitButtonText = (() => {
        if (postingComment) return editingCommentId != null ? "Updating…" : "Posting…";
        return editingCommentId != null ? "Update Comment" : "Post Comment";
    })();

    if (!canUseComments) {
        return null;
    }

    return (
        <>
            <div className={`section-title ${sectionTitleClassName}`.trim()}>
                <h5 className="_600">Comments</h5>
                <div className="section-actions"></div>
            </div>
            <div className="comments">
                {commentsLoading ? (
                    <AppLoader inline />
                ) : (
                    showList &&
                    list.map((c) => (
                        <div className="comment-item" key={c.id}>
                            <div className={`comment-avatar user-bg-${(c.id % 4) + 1}`}>{c.initials}</div>
                            <div className="comment-body">
                                <div className="comment-content">{c.text}</div>
                                <div className="comment-meta">
                                    <span className="comment-user">
                                        <Icon.Person size={12} className="icon-mini" /> {c.email}
                                    </span>
                                    <span className="comment-time">
                                        <Icon.Clock size={12} className="icon-mini" /> {c.time}
                                    </span>
                                </div>
                            </div>
                            {/*{c.editable !== false ? (*/}
                            {!readOnly && (
                                <>
                                    <button
                                        type="button"
                                        className="btn btn-link p-1 border-0 bg-transparent ms-auto align-self-center flex-shrink-0"
                                        aria-label="Edit comment"
                                        onClick={(e) => {
                                            e.preventDefault();
                                            e.stopPropagation();
                                            const full = comments.find((x) => x.id === c.id);
                                            if (full) handleStartEditComment(full);
                                        }}
                                    >
                                        <Icon.PencilSquare size={18} />
                                    </button>

                                    <button
                                        type="button"
                                        className="btn btn-link p-1 border-0 bg-transparent align-self-center text-danger flex-shrink-0"
                                        aria-label="Delete comment"
                                        disabled={deletingCommentId === c.id}
                                        onClick={(e) => {
                                            e.preventDefault();
                                            e.stopPropagation();
                                            const full = comments.find((x) => x.id === c.id);
                                            if (full) handleDeleteComment(full);
                                        }}
                                    >
                                        {deletingCommentId === c.id
                                            ? <span className="spinner-border spinner-border-sm" />
                                            : <Icon.Trash size={18} />
                                        }
                                    </button>
                                </>
                            )}
                            {/*) : null}*/}
                        </div>
                    ))
                )}

                <Divider className="divider-1" />
                {!readOnly && (
                    <div className="comment-form">
                        <div className="comment-item">
                            <div className="comment-avatar user-bg-2 align-self-start">You</div>
                            <div className="comment-body flex-fill">
                                <div className="row">
                                    <div className="col-md-6">
                                        <TextArea
                                            className="comments-textarea"
                                            placeholder="Add a comment..."
                                            rows={3}
                                            value={newComment}
                                            maxLength={COMMENT_MAX_LENGTH}
                                            onChange={(val: string) => {
                                                setNewComment(val);
                                                if (val.trim() === "") {
                                                    setEditingCommentId(null);
                                                }
                                                if (commentError) setCommentError("");
                                            }}
                                        />
                                        {commentError ? (
                                            <span className="text-danger small d-block mt-1">{commentError}</span>
                                        ) : null}
                                    </div>
                                </div>
                                {globalpermission && (
                                    <div className="d-flex gap-2 mt-2">
                                        <Button className="btn btn-primary btn-sm"
                                            disabled={postingComment || !newComment.trim()}
                                            onClick={() => void handlePostOrUpdateComment()}
                                        >
                                            {submitButtonText}
                                        </Button>
                                        {editingCommentId != null && (
                                            <Button className="btn btn-secondary btn-sm"
                                                disabled={postingComment}
                                                onClick={() => {
                                                    setEditingCommentId(null);
                                                    setNewComment("");
                                                    setCommentError("");
                                                }}
                                            >
                                                Cancel
                                            </Button>
                                        )}
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </>
    );
});

export default Comments;

