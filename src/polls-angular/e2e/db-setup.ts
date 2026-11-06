import { execFileSync } from 'node:child_process';
import { join } from 'node:path';
import { settings } from './settings';

const migrations = join(__dirname, '..', '..', 'polls', 'db', 'migrations.sql');

/** Rows the test creates; parents last (foreign keys). Data Protection keys and the migration history stay. */
const resetSql = `
SET NOCOUNT ON;
DELETE FROM answers;
DELETE FROM user_poll;
DELETE FROM questions;
DELETE FROM polls;
DELETE FROM users;
DELETE FROM threshold_warnings;`;

function sqlcmd(args: string[]): void {
  execFileSync(
    'sqlcmd',
    ['-S', settings.sqlServer, '-d', settings.database, '-U', settings.ddlUser, '-I', '-b', ...args],
    // the password goes through the environment, not the command line
    { stdio: 'inherit', env: { ...process.env, SQLCMDPASSWORD: settings.ddlPassword } },
  );
}

/**
 * Playwright global setup: brings the test database up to date (idempotent migration script, D13)
 * and, with E2E_RESET_DB=true, deletes all test data so every run starts empty.
 */
export default function dbSetup(): void {
  // Guard against pointing the reset at a real database by mistake
  if (settings.resetDb && !/e2e|test/i.test(settings.database)) {
    throw new Error(
      `E2E_RESET_DB=true deletes all rows; refusing to run on "${settings.database}" ` +
        '(the database name must contain "e2e" or "test").',
    );
  }

  console.log(`[e2e] applying migrations to ${settings.database}`);
  sqlcmd(['-i', migrations]);

  if (settings.resetDb) {
    console.log(`[e2e] deleting test data in ${settings.database}`);
    sqlcmd(['-Q', resetSql]);
  }
}
