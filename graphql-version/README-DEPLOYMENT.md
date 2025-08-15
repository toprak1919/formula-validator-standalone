# Deployment Instructions for Render

## Prerequisites
1. Create a free account at [render.com](https://render.com)
2. Push your code to a GitHub repository

## Deployment Steps

### Option 1: Using render.yaml (Recommended)
1. Push all your code including the `render.yaml` file to GitHub
2. Go to [Render Dashboard](https://dashboard.render.com)
3. Click "New" → "Blueprint"
4. Connect your GitHub repository
5. Render will automatically detect the `render.yaml` and create both services

### Option 2: Manual Deployment

#### Deploy Backend (.NET GraphQL API)
1. Go to [Render Dashboard](https://dashboard.render.com)
2. Click "New" → "Web Service"
3. Connect your GitHub repository
4. Configure:
   - **Name**: `formula-validator-api`
   - **Root Directory**: `graphql-version/backend`
   - **Runtime**: Docker
   - **Build Command**: (Leave empty - uses Dockerfile)
   - **Start Command**: (Leave empty - uses Dockerfile)
5. Click "Create Web Service"
6. Wait for deployment (first build takes ~5-10 minutes)
7. Note your API URL: `https://formula-validator-api.onrender.com`

#### Deploy Frontend (Static Site)
1. Go to [Render Dashboard](https://dashboard.render.com)
2. Click "New" → "Static Site"
3. Connect your GitHub repository
4. Configure:
   - **Name**: `formula-validator-frontend`
   - **Root Directory**: `graphql-version/frontend`
   - **Build Command**: (Leave empty)
   - **Publish Directory**: `.`
5. Click "Create Static Site"

### Post-Deployment Configuration

1. **Update Frontend API URL**:
   - Edit `frontend/config.js`
   - Replace `your-api-name` with your actual Render backend service name:
   ```javascript
   PRODUCTION_URL: 'https://formula-validator-api.onrender.com/graphql'
   ```
   - Commit and push the change

2. **Test Your Deployment**:
   - Backend GraphQL Playground: `https://formula-validator-api.onrender.com/graphql`
   - Frontend: `https://formula-validator-frontend.onrender.com`

## Important Notes

- **Free Tier Limitations**:
  - Services spin down after 15 minutes of inactivity
  - First request after spin-down takes ~30 seconds
  - 750 hours/month of free runtime

- **Environment Variables** (if needed):
  - Set in Render Dashboard → Service → Environment
  - Backend already configured to use PORT environment variable

- **Custom Domains**:
  - Available in free tier
  - Configure in Service Settings → Custom Domains

## Troubleshooting

1. **Backend not starting**: Check Render logs for build errors
2. **Frontend can't reach backend**: Verify CORS is enabled and API URL is correct
3. **Slow initial load**: Normal for free tier - service needs to spin up

## Local Testing of Docker Build
```bash
cd graphql-version/backend
docker build -t formula-validator .
docker run -p 5000:80 formula-validator
```

## Repository Structure Required
```
graphql-version/
├── render.yaml
├── test-cases.json
├── backend/
│   ├── Dockerfile
│   ├── .dockerignore
│   ├── FormulaValidatorAPI/
│   └── FormulaValidatorAPI.Tests/
└── frontend/
    ├── index.html
    └── config.js
```