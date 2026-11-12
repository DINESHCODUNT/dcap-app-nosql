import React from 'react';

if (!(React as any).createFactory) {
    (React as any).createFactory = (type: any) => {
        return React.createElement.bind(null, type);
    };
}