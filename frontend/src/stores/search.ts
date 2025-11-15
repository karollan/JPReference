import { defineStore } from 'pinia'

export const useSearchStore = defineStore('search', () => {
    //State
    

    const loading = ref<boolean>(false);
    const loadingMore = ref<boolean>(false);
    const error = ref<string | null>(null);


    //Actions

    //Getters


    return {

    }
});