import type { FilterParseState } from './filterParser'
import type { FilterDefinition, FilterOperator } from './filters'
import { getAllFilterKeys, getFilterDefinition } from './filters'

export interface Suggestion {
  type: 'filter-key' | 'operator' | 'value' | 'hint'
  label: string
  value: string
  description?: string
}

/**
 * Generate context-sensitive suggestions based on filter state
 */
export function generateSuggestions (
  state: FilterParseState,
  def: FilterDefinition | undefined,
): Suggestion[] {
  // Stage: key - show matching filter keys
  if (state.stage === 'key') {
    return generateFilterKeySuggestions(state.key)
  }

  if (!def) {
    return []
  }

  // Stage: operator - show operators (only for multi-op filters)
  if (state.stage === 'operator') {
    if (def.type === 'multi-op' && def.operators) {
      return generateOperatorSuggestions(def.operators)
    }
    // If not multi-op, skip operator stage
    return []
  }

  // Stage: value - show enum values or hints
  if (state.stage === 'value') {
    if (def.type === 'enum' && def.enumValues) {
      return generateEnumValueSuggestions(def, state.value || '')
    }
    // For equality and range, no suggestions - just validate inline
    return []
  }

  // Stage: range-separator - show hint for upper bound
  if (state.stage === 'range-separator') {
    if (def.type === 'range' && def.max !== undefined && state.rangeMin !== undefined) {
      return [{
        type: 'hint',
        label: `Enter upper bound (${state.rangeMin}-${def.max})`,
        value: '',
        description: `Maximum value: ${def.max}`,
      }]
    }
    return []
  }

  // Stage: range-max - show commit hint
  if (state.stage === 'range-max') {
    return [{
      type: 'hint',
      label: 'Press Space or Enter to commit',
      value: '',
      description: '',
    }]
  }

  return []
}

/**
 * Generate filter key suggestions matching the input
 */
function generateFilterKeySuggestions (partialKey: string): Suggestion[] {
  const allKeys = getAllFilterKeys()
  const partialLower = partialKey.toLowerCase()

  const matching = allKeys
    .filter(key => key.toLowerCase().startsWith(partialLower))
    .map(key => {
      const def = getFilterDefinition(key)
      return {
        key,
        def,
        exact: key.toLowerCase() === partialLower,
        length: key.length,
      }
    })
    .toSorted((a, b) => {
      // Exact match first
      if (a.exact && !b.exact) {
        return -1
      }
      if (b.exact && !a.exact) {
        return 1
      }
      // Then by length (shorter = more relevant)
      if (a.length !== b.length) {
        return a.length - b.length
      }
      // Finally alphabetically
      return a.key.localeCompare(b.key)
    })
    .slice(0, 10)

  return matching.map(({ key, def }) => ({
    type: 'filter-key' as const,
    label: key,
    value: key,
    description: def?.description,
  }))
}

/**
 * Generate operator suggestions for multi-op filters
 */
function generateOperatorSuggestions (operators: FilterOperator[]): Suggestion[] {
  const operatorLabels: Record<FilterOperator, string> = {
    '=': 'Equals',
    '>': 'Greater than',
    '<': 'Less than',
    'range': 'Range',
  }

  return operators.map(op => ({
    type: 'operator' as const,
    label: `${op} (${operatorLabels[op]})`,
    value: op,
    description: operatorLabels[op],
  }))
}

/**
 * Generate enum value suggestions
 */
function generateEnumValueSuggestions (
  def: FilterDefinition,
  partialValue: string,
): Suggestion[] {
  if (def.type !== 'enum' || !def.enumValues) {
    return []
  }

  const partialLower = partialValue.toLowerCase()
  const matching = def.enumValues
    .map(String)
    .filter(val => val.toLowerCase().startsWith(partialLower))
    .toSorted((a, b) => {
      const aNum = Number.parseInt(a, 10)
      const bNum = Number.parseInt(b, 10)
      if (!Number.isNaN(aNum) && !Number.isNaN(bNum)) {
        return aNum - bNum
      }
      return a.localeCompare(b)
    })
    .slice(0, 10)

  return matching.map(val => ({
    type: 'value' as const,
    label: val,
    value: val,
    description: def.description,
  }))
}
