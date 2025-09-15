// Configuration for API endpoint
const API_CONFIG = {
    // For local development
    LOCAL_URL: 'http://localhost:5001/graphql',
    
    // For production: default to same-origin '/graphql' so Vercel can proxy
    // You can still hardcode a full URL here if not using the Vercel proxy.
    PRODUCTION_URL: '/graphql',
    
    // Automatically use production URL if not on localhost
    get URL() {
        const isLocal = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
        if (isLocal) return this.LOCAL_URL;
        // Allow overriding via meta tag when deployed as a static site (e.g., Render)
        const meta = typeof document !== 'undefined' ? document.querySelector('meta[name="backend-url"]') : null;
        if (meta && meta.content) return meta.content;
        return this.PRODUCTION_URL;
    }
};
