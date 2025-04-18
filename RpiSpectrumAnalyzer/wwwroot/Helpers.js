class Helpers{
    map(value, fromMin, fromMax, toMin, toMax){
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }
    
    
    hexStringToRgbJson(hex) {
        // Convert HEX to RGB
        const bigint = parseInt(hex.slice(1), 16);
        return {
            r: (bigint >> 16) & 255,
            g: (bigint >> 8) & 255,
            b: bigint & 255
        };
    }

    rgbToHexString(r, g, b) {
        // Convert RGB to HEX
        return `#${((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1).toUpperCase()}`;
    }

    generateGradient(startColorAsHexString, endColorAsHexString, gradientCount) {
        const startRgb = this.hexStringToRgbJson(startColorAsHexString);
        const endRgb = this.hexStringToRgbJson(endColorAsHexString);

        const gradient = [];

        for (let i = 0; i < gradientCount; i++) {
            const t = i / (gradientCount - 1); // Interpolation factor
            const r = Math.round(startRgb.r + t * (endRgb.r - startRgb.r));
            const g = Math.round(startRgb.g + t * (endRgb.g - startRgb.g));
            const b = Math.round(startRgb.b + t * (endRgb.b - startRgb.b));
            gradient.push(this.rgbToHexString(r, g, b));
        }

        return gradient;
    }
}