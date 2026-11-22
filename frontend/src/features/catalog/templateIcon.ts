import type { IconName } from '../../shared/ui/Icon'
import type { Template } from '../../shared/api/contracts'

export function templateIcon(template: Template): IconName {
  if (template.type === 'Bottleneck') return 'hourglass'
  const text = `${template.title} ${template.improvementObject}`.toLocaleLowerCase('ru')
  if (/безопас|охран|риск|травм/.test(text)) return 'shield'
  if (/врем|ожидан|скорост|чек-лист/.test(text)) return 'clock'
  if (/качеств|брак|стандарт/.test(text)) return 'diamond'
  if (/эконом|ресурс|энерг|материал/.test(text)) return 'leaf'
  if (/модерниз|оборудован|автоматиз/.test(text)) return 'settings'
  return 'file'
}
