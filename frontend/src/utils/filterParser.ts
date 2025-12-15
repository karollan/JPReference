import type { FilterDefinition, FilterOperator } from './filters'

export type FilterStage = 'key' | 'operator' | 'value' | 'range-separator' | 'range-max' | 'complete'

export interface FilterParseState {
  stage: FilterStage
  key: string
  operator?: FilterOperator
  value?: string
  rangeMin?: number
  rangeMax?: number
  triggerIndex: number
  cursorIndex: number
  filterText: string // The text after the # character
}

/**
 * Parse filter state from text and cursor position
 * Finds the last # before cursor and parses the filter being constructed
 */
export function parseFilterState (
  text: string,
  cursorIndex: number,
): FilterParseState | null {
  // Find the last # before or at cursor position
  let lastHashIndex = -1
  for (let i = cursorIndex; i >= 0; i--) {
    if (text[i] === '#') {
      lastHashIndex = i
      break
    }
  }

  if (lastHashIndex === -1) {
    return null
  }

  // Check if # is preceded by non-whitespace (invalid)
  if (lastHashIndex > 0) {
    const charBefore = text[lastHashIndex - 1]
    if (charBefore && /\S/.test(charBefore)) {
      return null
    }
  }

  // Get text after #
  const filterText = text.slice(lastHashIndex + 1, cursorIndex)

  // Check if there's a space in the filter text (means we're past this filter)
  const spaceIndex = filterText.indexOf(' ')
  if (spaceIndex !== -1) {
    return null
  }

  // Parse the filter text
  const colonIndex = filterText.indexOf(':')

  if (colonIndex === -1) {
    // Stage: key (#key or #key|)
    return {
      stage: 'key',
      key: filterText,
      triggerIndex: lastHashIndex,
      cursorIndex,
      filterText,
    }
  }

  const key = filterText.slice(0, colonIndex)
  const afterColon = filterText.slice(colonIndex + 1)

  // Check for range separator
  const dashIndex = afterColon.indexOf('-')

  if (dashIndex === -1) {
    // Stage: value (#key:value or #key:value|)
    const value = afterColon

    // Check if value is empty (operator stage for multi-op filters)
    if (value === '') {
      return {
        stage: 'operator',
        key,
        triggerIndex: lastHashIndex,
        cursorIndex,
        filterText,
      }
    }

    return {
      stage: 'value',
      key,
      value,
      triggerIndex: lastHashIndex,
      cursorIndex,
      filterText,
    }
  }

  // Range detected
  const rangeMinStr = afterColon.slice(0, dashIndex)
  const rangeMaxStr = afterColon.slice(dashIndex + 1)

  if (rangeMaxStr === '') {
    // Stage: range-separator (#key:1-|)
    const rangeMin = Number.parseInt(rangeMinStr, 10)
    return {
      stage: 'range-separator',
      key,
      rangeMin: Number.isNaN(rangeMin) ? undefined : rangeMin,
      triggerIndex: lastHashIndex,
      cursorIndex,
      filterText,
    }
  }

  // Stage: range-max (#key:1-12 or #key:1-12|)
  const rangeMin = Number.parseInt(rangeMinStr, 10)
  const rangeMax = Number.parseInt(rangeMaxStr, 10)

  return {
    stage: 'range-max',
    key,
    rangeMin: Number.isNaN(rangeMin) ? undefined : rangeMin,
    rangeMax: Number.isNaN(rangeMax) ? undefined : rangeMax,
    triggerIndex: lastHashIndex,
    cursorIndex,
    filterText,
  }
}

/**
 * Check if filter syntax is valid for the given definition
 */
export function isValidFilterSyntax (
  def: FilterDefinition,
  state: FilterParseState,
): boolean {
  if (state.stage === 'key') {
    return true // Key stage is always valid (we're still typing)
  }

  if (state.stage === 'operator') {
    return true // Operator stage is valid (we're selecting operator)
  }

  if (def.type === 'boolean') {
    // Boolean filters are complete after key
    return state.key === def.key
  }

  if (state.stage === 'value') {
    if (!state.value) {
      return false
    }

    if (def.valueType === 'int') {
      const num = Number.parseInt(state.value, 10)
      if (Number.isNaN(num)) {
        return false
      }

      if (def.type === 'enum') {
        const enumVals = def.enumValues as number[] | undefined
        return enumVals?.includes(num) ?? false
      }

      if (def.type === 'equality' || def.type === 'range') {
        if (def.min !== undefined && num < def.min) {
          return false
        }
        if (def.max !== undefined && num > def.max) {
          return false
        }
        return true
      }
    }

    return true
  }

  if (state.stage === 'range-separator') {
    if (state.rangeMin === undefined) {
      return false
    }

    if (def.min !== undefined && state.rangeMin < def.min) {
      return false
    }
    if (def.max !== undefined && state.rangeMin > def.max) {
      return false
    }

    return true
  }

  if (state.stage === 'range-max') {
    if (state.rangeMin === undefined || state.rangeMax === undefined) {
      return false
    }

    if (def.min !== undefined && state.rangeMin < def.min) {
      return false
    }
    if (def.max !== undefined && state.rangeMin > def.max) {
      return false
    }
    if (def.min !== undefined && state.rangeMax < def.min) {
      return false
    }
    if (def.max !== undefined && state.rangeMax > def.max) {
      return false
    }
    if (state.rangeMin > state.rangeMax) {
      return false
    }

    return true
  }

  return false
}

/**
 * Format filter string from state
 */
export function formatFilterString (
  def: FilterDefinition,
  state: FilterParseState,
): string {
  if (def.type === 'boolean') {
    return `#${def.key}`
  }

  if (state.stage === 'value' && state.value) {
    return `#${def.key}:${state.value}`
  }

  if (state.stage === 'range-max' && state.rangeMin !== undefined && state.rangeMax !== undefined) {
    return `#${def.key}:${state.rangeMin}-${state.rangeMax}`
  }

  return `#${state.key}`
}

/**
 * Check if a filter is complete (ready to commit)
 */
export function isFilterComplete (
  def: FilterDefinition,
  state: FilterParseState,
): boolean {
  if (def.type === 'boolean') {
    return state.stage === 'key' && state.key === def.key
  }

  if (def.type === 'enum' || def.type === 'equality') {
    return state.stage === 'value' && isValidFilterSyntax(def, state)
  }

  if (def.type === 'range') {
    // Range filters can complete at 'value' stage (single value like #stroke:1)
    // or at 'range-max' stage (range like #stroke:1-3)
    return (state.stage === 'value' || state.stage === 'range-max') && isValidFilterSyntax(def, state)
  }

  if (def.type === 'multi-op') {
    // Multi-op filters complete after operator + value
    return state.stage === 'value' && state.operator !== undefined && isValidFilterSyntax(def, state)
  }

  return false
}