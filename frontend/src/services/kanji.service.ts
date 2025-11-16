import axios from "axios";
import type { KanjiResponse, KanjiSummary } from "@/types/Kanji";


const API_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api"

export class KanjiService {
    static async fetchKanjis(query: string | null, page: number = 1, pageSize: number = 50): Promise<KanjiResponse> {
        const response = await axios.get<KanjiResponse>(`${API_URL}/kanji`, {
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

    static async fetchKanji(guid: string): Promise<KanjiSummary> {
        const { data } = await axios.get(`${API_URL}/kanji/${guid}`)
        return data;
    }
}