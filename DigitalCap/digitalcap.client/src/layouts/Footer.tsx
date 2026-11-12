import React from 'react';


const Footer: React.FC = () => {
    return (
        <div className=' '>
            <div className="copyright-footer">
                <div className="d-flex flex-column flex-md-row justify-content-between align-items-center mb-0 px-2 py-2">
                    <span className="copyright _500 tx-12 text-center">&copy; 2026. American Bureau of Shipping. All rights reserved.</span>
                    <div className="d-flex gap-2 justify-content-center mt-2 mt-md-0">
                        <span id="releaseBuildTime">Ver_1</span>
                        <span className="tx-14" id="releaseVersion">2.1</span>
                    </div>
                </div>
            </div>
        </div>
    );
};
export default Footer;
