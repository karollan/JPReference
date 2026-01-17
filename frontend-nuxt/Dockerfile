# Development stage
FROM node:24-alpine AS development
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install
COPY . .
EXPOSE 3000
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]

# Build stage
FROM node:24-alpine AS build
WORKDIR /app
ARG NUXT_PUBLIC_ADSENSE_CLIENT_ID
ARG NUXT_PUBLIC_ADSENSE_SLOT_ID
ENV NUXT_PUBLIC_ADSENSE_CLIENT_ID=$NUXT_PUBLIC_ADSENSE_CLIENT_ID
ENV NUXT_PUBLIC_ADSENSE_SLOT_ID=$NUXT_PUBLIC_ADSENSE_SLOT_ID
COPY package.json package-lock.json ./
RUN npm install
COPY . .
RUN npm run build

# Production stage
FROM node:24-alpine AS final
WORKDIR /app
COPY --from=build /app/.output /app/.output
ENV NODE_ENV=production
# Limit Node.js heap to prevent OOM on small VPS (adjust as needed)
ENV NODE_OPTIONS="--max-old-space-size=512"
EXPOSE 3000
CMD ["node", ".output/server/index.mjs"]
