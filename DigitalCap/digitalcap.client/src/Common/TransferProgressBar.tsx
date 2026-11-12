import React from 'react';

export interface TransferProgressBarProps {
    percent: number;
    height?: string;
    variant?: string;
    showLabel?: boolean;
}

export const TransferProgressBar: React.FC<TransferProgressBarProps> = ({
    percent,
    height = '20px',
    variant = 'bg-primary',
    showLabel = true
}) => {
    const clamped = Math.min(100, Math.max(0, Math.round(percent)));
    const isThin = height === '6px' || height === '10px';

    return (
        <div className="d-flex align-items-center gap-2 w-100">
            <div className="progress flex-grow-1" style={{ height }}>
                <div
                    className={`progress-bar ${variant}`}
                    role="progressbar"
                    style={{
                        width: `${clamped}%`,
                        transition: 'width 0.3s ease'
                    }}
                    aria-valuenow={clamped}
                    aria-valuemin={0}
                    aria-valuemax={100}
                >
                    {!isThin && showLabel && clamped >= 10 ? `${clamped}%` : null}
                </div>
            </div>
            {showLabel && (isThin || clamped < 10) && (
                <span className="small text-muted" style={{ minWidth: '35px', textAlign: 'right', fontSize: '12px' }}>
                    {clamped}%
                </span>
            )}
        </div>
    );
};

export default TransferProgressBar;
