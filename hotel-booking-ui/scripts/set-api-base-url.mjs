import { writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const fallbackApiBaseUrl = 'https://your-backend-api-domain.example.com/api';
const apiBaseUrl = (process.env.API_BASE_URL || fallbackApiBaseUrl).trim();

const normalizedApiBaseUrl = apiBaseUrl.replace(/\/$/, '');

if (!/^https?:\/\//.test(normalizedApiBaseUrl)) {
  console.warn(
    `⚠️ API_BASE_URL should be an absolute URL. Received: "${normalizedApiBaseUrl}". Falling back to placeholder.`,
  );
}

const outputPath = resolve(process.cwd(), 'src/environments/environment.production.ts');
const fileContent = `export const environment = {\n  production: true,\n  apiBaseUrl: '${normalizedApiBaseUrl}',\n};\n`;

writeFileSync(outputPath, fileContent, 'utf8');
console.log(`✅ Production API base URL set to: ${normalizedApiBaseUrl}`);
