# ownidentity-admin

This Vue 3 application uses Vite, Vitest, and Bun 1.3.14.

## Recommended IDE Setup

[VS Code](https://code.visualstudio.com/) + [Vue (Official)](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (and disable Vetur).

## Recommended Browser Setup

- Chromium-based browsers (Chrome, Edge, Brave, etc.):
  - [Vue.js devtools](https://chromewebstore.google.com/detail/vuejs-devtools/nhdogjmejiglipccpnnnanhbledajbpd)
  - [Turn on Custom Object Formatter in Chrome DevTools](http://bit.ly/object-formatters)
- Firefox:
  - [Vue.js devtools](https://addons.mozilla.org/en-US/firefox/addon/vue-js-devtools/)
  - [Turn on Custom Object Formatter in Firefox DevTools](https://fxdx.dev/firefox-devtools-custom-object-formatters/)

## Type Support for `.vue` Imports in TS

TypeScript cannot handle type information for `.vue` imports by default, so we replace the `tsc` CLI with `vue-tsc` for type checking. In editors, we need [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) to make the TypeScript language service aware of `.vue` types.

## Customize configuration

See [Vite Configuration Reference](https://vite.dev/config/).

## Project Setup

```sh
bun install --frozen-lockfile --ignore-scripts
```

`bunfig.toml` persists `ignoreScripts = true`, blocking root and dependency lifecycle scripts. The explicit flag is retained in deterministic install paths as defense in depth. Do not add `trustedDependencies` or exceptions without an explicit security decision; packages that require install scripts are incompatible with the current policy.

### Compile and Hot-Reload for Development

```sh
bun run dev
```

### Type-Check, Compile and Minify for Production

```sh
bun run build
```

### Run Unit Tests with [Vitest](https://vitest.dev/)

```sh
bun run test:unit -- --run
```

### Lint with [ESLint](https://eslint.org/)

```sh
bun run lint
```

Type-check and audit independently with `bun run type-check` and `bun audit`. Intentional dependency updates use Bun to regenerate `bun.lock` without `--frozen-lockfile`, but scripts remain blocked by `bunfig.toml`. Blocking scripts does not establish package or Vite-plugin trust; review dependency changes and audit the resolved graph.
