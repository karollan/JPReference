import type { FilterDefinition } from './filters'

export interface ValidationResult {
  valid: boolean
  error?: string
}

/**
 * Validate a filter value against its definition
 */
export function validateFilterValue (
  def: FilterDefinition,
  value: string,
): ValidationResult {
  if (def.type === 'boolean') {
    return { valid: true }
  }

  if (!value) {
    return { valid: false, error: 'Value required' }
  }

  if (def.valueType === 'string' && def.type === 'enum' && !(def.enumValues as string[])?.includes(value)) {
    return {
      valid: false,
      error: `Must be one of: ${def.enumValues?.join(', ')}`,
    }
  }

  if (def.valueType === 'int') {
    const num = Number.parseInt(value, 10)
    if (Number.isNaN(num)) {
      return { valid: false, error: 'Must be a number' }
    }

    if (def.type === 'enum' && !(def.enumValues as number[])?.includes(num)) {
      return {
        valid: false,
        error: `Must be one of: ${def.enumValues?.join(', ')}`,
      }
    }

    if (def.min !== undefined && num < def.min) {
      return {
        valid: false,
        error: `Must be at least ${def.min}`,
      }
    }

    if (def.max !== undefined && num > def.max) {
      return {
        valid: false,
        error: `Must be at most ${def.max}`,
      }
    }
  }

  return { valid: true }
}

/**
 * Validate a range filter
 */
export function validateRange (
  def: FilterDefinition,
  min: number,
  max?: number,
): ValidationResult {
  if (def.type !== 'range') {
    return { valid: false, error: 'Not a range filter' }
  }

  if (def.min !== undefined && min < def.min) {
    return {
      valid: false,
      error: `Minimum must be at least ${def.min}`,
    }
  }

  if (def.max !== undefined && min > def.max) {
    return {
      valid: false,
      error: `Minimum must be at most ${def.max}`,
    }
  }

  if (max !== undefined) {
    if (def.min !== undefined && max < def.min) {
      return {
        valid: false,
        error: `Maximum must be at least ${def.min}`,
      }
    }

    if (def.max !== undefined && max > def.max) {
      return {
        valid: false,
        error: `Maximum must be at most ${def.max}`,
      }
    }

    if (min > max) {
      return {
        valid: false,
        error: 'Minimum must be less than or equal to maximum',
      }
    }
  }

  return { valid: true }
}

/**
 * Get validation error message for a filter state (if any)
 */
export function getFilterValidationError (
  def: FilterDefinition,
  value?: string,
  rangeMin?: number,
  rangeMax?: number,
): string | undefined {
  if (def.type === 'range') {
    // Range filters can have single values (treated as value-value) or ranges
    if (rangeMin !== undefined) {
      // Range format: 1-3
      const result = validateRange(def, rangeMin, rangeMax)
      return result.error
    } else if (value !== undefined) {
      // Single value format: 1 (treated as 1-1)
      const result = validateFilterValue(def, value)
      return result.error
    }
  } else if (value !== undefined) {
    const result = validateFilterValue(def, value)
    return result.error
  }

  return undefined
}
