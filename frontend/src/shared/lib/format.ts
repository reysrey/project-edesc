export function formatDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? 'Дата не указана'
    : new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' }).format(
        date,
      )
}
export function csvCell(value: string) {
  // Prevent formula execution when a downloaded CSV is opened in a spreadsheet.
  return `"${(/^[\s]*[=+\-@\t\r]/.test(value) ? "'" + value : value).replaceAll('"', '""')}"`
}
const pluralRules = new Intl.PluralRules('ru-RU')
export function formatCount(count: number, forms: readonly [string, string, string]) {
  const form = pluralRules.select(count)
  return `${count} ${forms[form === 'one' ? 0 : form === 'few' ? 1 : 2]}`
}
