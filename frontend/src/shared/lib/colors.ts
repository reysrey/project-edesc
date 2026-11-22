function rgb(hex: string) {
  return hex
    .replace('#', '')
    .match(/.{2}/g)!
    .map((value) => parseInt(value, 16))
}
function luminance(hex: string) {
  const channels = rgb(hex).map((value) => {
    const v = value / 255
    return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4
  })
  return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722
}
export function contrastRatio(a: string, b: string) {
  const first = luminance(a),
    second = luminance(b)
  return (Math.max(first, second) + 0.05) / (Math.min(first, second) + 0.05)
}
export function onColor(background: string) {
  return contrastRatio('#000000', background) > contrastRatio('#ffffff', background)
    ? '#000000'
    : '#ffffff'
}
export function readableColor(color: string, background: string) {
  if (contrastRatio(color, background) >= 4.5) return color
  const from = rgb(color),
    to = rgb(onColor(background))
  for (let step = 1; step <= 40; step++) {
    const candidate =
      '#' +
      from
        .map((value, index) =>
          Math.round(value + ((to[index] - value) * step) / 40)
            .toString(16)
            .padStart(2, '0'),
        )
        .join('')
    if (contrastRatio(candidate, background) >= 4.5) return candidate
  }
  return onColor(background)
}
