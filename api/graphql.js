// Simple proxy to forward /graphql requests to the external .NET backend.
// Configure BACKEND_URL in Vercel Project Settings (e.g., https://your-backend.example.com/graphql)

module.exports = async (req, res) => {
  const base = process.env.BACKEND_URL || 'http://localhost:5001/graphql';

  const qIndex = req.url.indexOf('?');
  const target = qIndex !== -1 ? base + req.url.slice(qIndex) : base;

  // Collect request body (for POST/others)
  const chunks = [];
  req.on('data', chunk => chunks.push(chunk));

  req.on('end', async () => {
    const bodyBuffer = Buffer.concat(chunks);

    const headers = { ...req.headers };
    // Remove hop-by-hop and problematic headers when proxying
    delete headers.host;
    delete headers.connection;
    delete headers['content-length'];
    delete headers['accept-encoding'];

    const init = {
      method: req.method,
      headers,
    };

    if (!['GET', 'HEAD'].includes(req.method)) {
      if (bodyBuffer.length) init.body = bodyBuffer;
    }

    try {
      const response = await fetch(target, init);

      // Forward status and selected headers
      res.statusCode = response.status;
      response.headers.forEach((value, key) => {
        if (key.toLowerCase() === 'content-encoding') return; // avoid double-encoding issues
        res.setHeader(key, value);
      });

      const buf = Buffer.from(await response.arrayBuffer());
      res.end(buf);
    } catch (err) {
      res.statusCode = 502;
      res.setHeader('content-type', 'application/json');
      res.end(JSON.stringify({ error: 'Bad gateway to backend', details: String(err) }));
    }
  });
};
