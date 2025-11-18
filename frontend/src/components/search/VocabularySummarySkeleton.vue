<template>
    <v-card
        class="pa-4 mb-4"
        outlined
    >
        <v-row>
            <v-col cols="4">
                <v-skeleton-loader type="text" width="60%"></v-skeleton-loader>
                <v-skeleton-loader type="heading" class="mt-1"></v-skeleton-loader>
                <div class="d-flex gap-1 mt-2">
                    <v-skeleton-loader type="chip" width="60px"></v-skeleton-loader>
                    <v-skeleton-loader type="chip" width="70px"></v-skeleton-loader>
                </div>
            </v-col>
            <v-col cols="8" class="text-left">
                <v-skeleton-loader type="text" width="90%"></v-skeleton-loader>
                <v-skeleton-loader type="text" width="85%" class="mt-2"></v-skeleton-loader>
                <v-skeleton-loader type="text" width="80%" class="mt-2"></v-skeleton-loader>
            </v-col>
        </v-row>
    </v-card>
</template>

<script lang="ts" setup>
</script>

<style lang="scss" scoped>
</style>
