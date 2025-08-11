import typescript from 'rollup-plugin-typescript2';
import { resolve } from 'path';
import { readFileSync } from 'fs';

// Read package.json to get version and dependencies
const pkg = JSON.parse(readFileSync('./package.json', 'utf-8'));

// Base configuration shared across all builds
const baseConfig = {
  input: 'src/index.ts',
  external: [
    // Mark all dependencies as external so they're not bundled
    ...Object.keys(pkg.dependencies || {}),
    ...Object.keys(pkg.peerDependencies || {}),
  ],
  plugins: [
    typescript({
      typescript: require('typescript'),
      tsconfig: 'tsconfig.build.json',
      clean: true,
      exclude: ['**/*.test.ts', '**/*.spec.ts'],
    }),
  ],
};

// ESM build configuration
const esmConfig = {
  ...baseConfig,
  output: {
    file: 'dist/esm/index.js',
    format: 'es',
    sourcemap: true,
  },
  plugins: [
    ...baseConfig.plugins,
  ],
};

// CommonJS build configuration
const cjsConfig = {
  ...baseConfig,
  output: {
    file: 'dist/cjs/index.js',
    format: 'cjs',
    sourcemap: true,
    exports: 'named',
  },
  plugins: [
    ...baseConfig.plugins,
  ],
};

// UMD build configuration (for browser)
const umdConfig = {
  ...baseConfig,
  external: [], // Don't externalize dependencies for UMD build
  output: {
    file: 'dist/umd/easyauth.js',
    format: 'umd',
    name: 'EasyAuth',
    sourcemap: true,
    globals: {
      // Define globals for any external dependencies if needed
      // 'dependency-name': 'GlobalVariableName'
    },
  },
  plugins: [
    ...baseConfig.plugins,
  ],
};

// UMD minified build configuration
const umdMinConfig = {
  ...umdConfig,
  output: {
    ...umdConfig.output,
    file: 'dist/umd/easyauth.min.js',
  },
  plugins: [
    ...umdConfig.plugins,
    // Add terser for minification in production
    process.env.NODE_ENV === 'production' && require('rollup-plugin-terser').terser({
      compress: {
        drop_console: true,
        drop_debugger: true,
        pure_funcs: ['console.log', 'console.debug'],
      },
      mangle: {
        reserved: ['EasyAuth'], // Don't mangle the global name
      },
      output: {
        comments: false,
      },
    }),
  ].filter(Boolean),
};

// Export configuration based on format
export default (commandLineArgs) => {
  const format = commandLineArgs.format || process.env.ROLLUP_FORMAT;
  
  switch (format) {
    case 'es':
    case 'esm':
      return esmConfig;
    case 'cjs':
    case 'commonjs':
      return cjsConfig;
    case 'umd':
      return commandLineArgs.file && commandLineArgs.file.includes('.min.') 
        ? umdMinConfig 
        : umdConfig;
    default:
      // Return all configurations for multi-format builds
      return [esmConfig, cjsConfig, umdConfig, umdMinConfig];
  }
};