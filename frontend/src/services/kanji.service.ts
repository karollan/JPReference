import axios from "axios";
import type { Kanji } from "@/types/Kanji";

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api"

interface KanjiListResponse {
    items: Kanji[]
    totalCount: number
    page: number
    pageSize: number
}

export class KanjiService {
    static async fetchKanjis(jlptLevel: number[], search: string | null, page: number = 1, pageSize: number = 50): Promise<KanjiListResponse> {
        const response = await axios.get<Kanji[]>(`${API_URL}/kanji`, {
            params: {
                jlptLevel: jlptLevel,
                search: search,
                page: page,
                pageSize: pageSize
            },
            paramsSerializer: {
                indexes: null
            }
        })
        
        // Extract metadata from headers
        const totalCount = parseInt(response.headers['x-total-count'] || '0')
        const currentPage = parseInt(response.headers['x-page'] || '1')
        const currentPageSize = parseInt(response.headers['x-page-size'] || '50')
        
        // Ensure response.data is an array
        const kanjiArray = Array.isArray(response.data) ? response.data : []
        
        return {
            items: kanjiArray.map(k => ({
                id: k.id,
                character: k.character,
                meanings: k.meanings,
                readingsOn: k.readingsOn,
                readingsKun: k.readingsKun,
                strokeCount: k.strokeCount,
                grade: k.grade,
                frequency: k.frequency,
                jlptOld: k.jlptOld,
                jlptNew: k.jlptNew,
                codepoints: k.codepoints,
                radicals: k.radicals,
                variants: k.variants,
                radicalNames: k.radicalNames,
                dictionaryReferences: k.dictionaryReferences,
                queryCodes: k.queryCodes,
                nanori: k.nanori
            })),
            totalCount: totalCount,
            page: currentPage,
            pageSize: currentPageSize
        }
    }

    static async fetchKanji(guid: string): Promise<Kanji> {
        const { data } = await axios.get(`${API_URL}/kanji/${guid}`)
        return data;
    }
}