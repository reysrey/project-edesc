import { readFile, writeFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { resolve, dirname } from 'node:path'

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const target = resolve(root, 'docs/design/ideahub.html')
let html = await readFile(target, 'utf8')
const css = await readFile(resolve(root, 'frontend/src/app/accessibility.css'), 'utf8')
const sprite = (await readFile(resolve(root, 'frontend/public/ideahub-icons.svg'), 'utf8'))
  .replace('<svg xmlns="http://www.w3.org/2000/svg">', '<svg xmlns="http://www.w3.org/2000/svg" aria-hidden="true" focusable="false" width="0" height="0" style="position:absolute;overflow:hidden">')
  .replaceAll('id="', 'id="ih-')
const cssBlock = `/* ideahub-accessibility:start */\n${css}\n/* ideahub-accessibility:end */`
const svgBlock = `<!-- ideahub-icons:start -->\n${sprite}\n<!-- ideahub-icons:end -->`
html = html.includes('/* ideahub-accessibility:start */')
  ? html.replace(/\/\* ideahub-accessibility:start \*\/[\s\S]*?\/\* ideahub-accessibility:end \*\//, () => cssBlock)
  : html.replace('</style>', `${cssBlock}\n</style>`)
html = html.includes('<!-- ideahub-icons:start -->')
  ? html.replace(/<!-- ideahub-icons:start -->[\s\S]*?<!-- ideahub-icons:end -->/, () => svgBlock)
  : html.replace('<body>', `<body>\n${svgBlock}`)
await writeFile(target, html)
console.log('Synchronized standalone design icons and accessibility styles.')
