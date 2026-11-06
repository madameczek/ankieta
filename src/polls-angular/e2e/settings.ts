import { existsSync } from 'node:fs';
import { join } from 'node:path';

/** Settings from e2e/.env (template: .env.example). Never commit the .env file. */
const envFile = join(__dirname, '.env');
if (!existsSync(envFile)) {
  throw new Error(`Missing ${envFile} – copy .env.example and fill it in (see e2e/README.md).`);
}
process.loadEnvFile(envFile);

function required(name: string): string {
  const value = process.env[name]?.trim();
  if (!value || value.includes('<')) {
    throw new Error(`e2e/.env: ${name} is not set`);
  }
  return value;
}

export const settings = {
  sqlServer: required('E2E_SQL_SERVER'),
  database: required('E2E_SQL_DATABASE'),
  ddlUser: required('E2E_SQL_DDL_USER'),
  ddlPassword: required('E2E_SQL_DDL_PASSWORD'),
  dmlUser: required('E2E_SQL_DML_USER'),
  dmlPassword: required('E2E_SQL_DML_PASSWORD'),
  resetDb: (process.env['E2E_RESET_DB'] ?? 'true').trim().toLowerCase() === 'true',
  apiPort: Number(process.env['E2E_API_PORT'] ?? 5180),
  webPort: Number(process.env['E2E_WEB_PORT'] ?? 4300),
};

/** Connection string for the API under test (DML login). */
export const apiConnectionString =
  `Server=${settings.sqlServer};Database=${settings.database};User Id=${settings.dmlUser};` +
  `Password=${settings.dmlPassword};TrustServerCertificate=True`;
