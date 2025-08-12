module.exports = {
  root: true,
  parser: '@typescript-eslint/parser',
  plugins: ['@typescript-eslint'],
  extends: [
    'eslint:recommended',
  ],
  parserOptions: {
    ecmaVersion: 2020,
    sourceType: 'module',
  },
  rules: {
    'prefer-const': 'error',
    'no-var': 'error',
    'no-unused-vars': 'off',
    '@typescript-eslint/no-unused-vars': ['error', { 'argsIgnorePattern': '^_' }]
  },
  env: {
    browser: true,
    node: true,
    es2020: true,
    jest: true,
  },
  globals: {
    RequestInit: 'readonly',
    Headers: 'readonly',
    fetch: 'readonly',
    Response: 'readonly',
  },
};