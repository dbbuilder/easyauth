module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/src/test-setup.ts'],
  testEnvironment: 'jsdom',
  collectCoverageFrom: [
    'src/**/*.ts',
    '!src/**/*.spec.ts',
    '!src/**/*.d.ts',
    '!src/test-setup.ts',
    '!src/index.ts',
    '!src/**/index.ts'
  ],
  coverageDirectory: 'coverage',
  coverageReporters: ['html', 'text-summary', 'lcov'],
  testMatch: [
    '<rootDir>/src/**/*.spec.ts'
  ],
  modulePathIgnorePatterns: ['<rootDir>/dist'],
  moduleFileExtensions: ['ts', 'html', 'js', 'json'],
  transformIgnorePatterns: [
    'node_modules/(?!@angular|@ngrx|ngx-.*)'
  ],
  globals: {
    'ts-jest': {
      tsconfig: 'tsconfig.json',
      stringifyContentPathRegex: '\\.html$'
    }
  }
};