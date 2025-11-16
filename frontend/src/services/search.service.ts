import axios from "axios"
import type { GlobalSearchResponse } from "@/types/GlobalSearch";

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api"

export class SearchService {
    static async fetchGlobalSearch(query: string, page: number = 1, pageSize: number = 50): Promise<GlobalSearchResponse> {
        const response = await axios.get<GlobalSearchResponse>(`${API_URL}/Search`, {
            params: {
                query: query,
                page: page,
                pageSize: pageSize
            },
            paramsSerializer: {
                indexes: null
            }
        })
        return response.data;
    }
}