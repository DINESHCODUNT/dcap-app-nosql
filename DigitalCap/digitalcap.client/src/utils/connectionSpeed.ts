/**
 * The website always measures speed against its own /images/speed.jpg.
 */
async function getSpeedImageUrl(): Promise<string | null> {
    return `${window.location.origin}/images/speed.jpg`;
}

/**
 * Measures the user's connection speed using a speed test image (~7.3MB).
 * Returns the download speed in Mbps (rounded to 2 decimal places), or null if the measurement failed.
 */
export async function measureConnectionSpeed(): Promise<number | null> {
    const imageUrl = await getSpeedImageUrl();
    if (!imageUrl) return null;

    const downloadSize = 7300000; // bytes

    return new Promise<number | null>((resolve) => {
        const download = new Image();
        let startTime = 0;

        download.onload = () => {
            const endTime = Date.now();
            const duration = (endTime - startTime) / 1000;
            if (duration <= 0) {
                resolve(null);
                return;
            }
            const bitsLoaded = downloadSize * 8;
            const speedBps = bitsLoaded / duration;
            const speedKbps = speedBps / 1024;
            const speedMbps = speedKbps / 1024;
            const mbps = parseFloat(speedMbps.toFixed(2));
            resolve(isNaN(mbps) ? null : mbps);
        };

        download.onerror = () => {
            resolve(null);
        };

        startTime = Date.now();
        const cacheBuster = (imageUrl.includes("?") ? "&" : "?") + "nnn=" + startTime;
        download.src = imageUrl + cacheBuster;
    });
}

/**
 * Detects whether an error was caused by a lost network connection (e.g. no response, ERR_NETWORK, fetch TypeError).
 */
export function isNetworkConnectionError(err: any): boolean {
    if (!err) return false;
    if (err instanceof TypeError || err.name === "TypeError") return true;
    if (err.code === "ERR_NETWORK" || err.code === "ECONNABORTED" || err.code === "ENOTFOUND") return true;
    if (err.isAxiosError && !err.response) return true;
    if (typeof err.message === "string" && (
        err.message.toLowerCase().includes("network error") ||
        err.message.toLowerCase().includes("failed to fetch") ||
        err.message.toLowerCase().includes("networkrequestfailed")
    )) {
        return true;
    }
    return false;
}

export const MeasureConnectionSpeed = measureConnectionSpeed;
export default measureConnectionSpeed;
