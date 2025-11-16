export interface TagInfo {
    code: string
    description: string
    category: string
    type?: string
}

export interface PaginationMetadata {
    totalCount: number
    page: number
    pageSize: number
    totalPages: number
    hasPrevious: boolean
    hasNext: boolean
}