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
        const { data } = await axios.get<KanjiListResponse>(`${API_URL}/kanji`, {
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
        return {
            items: data.items.map(k => ({
                id: k.id,
                character: k.character,
                meanings: k.meanings,
                readingsOn: k.readingsOn,
                readingsKun: k.readingsKun,
                strokeCount: k.strokeCount,
                grade: k.grade,
                frequency: k.frequency,
                jlptOld: k.jlptOld,
                jlptNew: k.jlptNew
            })),
            totalCount: data.totalCount,
            page: data.page,
            pageSize: data.pageSize
        }
    }

    static async fetchKanji(guid: string): Promise<Kanji> {
        const { data } = await axios.get(`${API_URL}/kanji/${guid}`)
        return data;
    }
}