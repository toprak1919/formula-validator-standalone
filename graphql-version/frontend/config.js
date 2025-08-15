// Configuration for API endpoint
const API_CONFIG = {
    // For local development
    LOCAL_URL: 'http://localhost:5232/graphql',
    
    // For production (update this after deploying to Render)
    // Replace 'your-api-name' with your actual Render service name
    PRODUCTION_URL: 'https://your-api-name.onrender.com/graphql',
    
    // Automatically use production URL if not on localhost
    get URL() {
        return window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
            ? this.LOCAL_URL
            : this.PRODUCTION_URL;
    }
};