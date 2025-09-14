// Configuration for API endpoint
const API_CONFIG = {
    // For local development
    LOCAL_URL: 'http://localhost:5001/graphql',
    
    // For production - Update with your actual backend URL
    PRODUCTION_URL: 'https://your-api-url.com/graphql',
    
    // Automatically use production URL if not on localhost
    get URL() {
        return window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
            ? this.LOCAL_URL
            : this.PRODUCTION_URL;
    }
};