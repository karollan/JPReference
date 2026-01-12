# Base stage for dependencies
FROM node:24-alpine AS base
WORKDIR /app
COPY package.json yarn.lock ./
RUN yarn install --frozen-lockfile

# Development stage (used by dev docker-compose)
FROM base AS development
COPY . .
EXPOSE 3000
CMD ["yarn", "dev", "--host", "0.0.0.0", "--port", "3000"]

# Build stage
FROM base AS build
ARG VITE_API_URL
ARG VITE_ADSENSE_CLIENT_ID
ARG VITE_ADSENSE_SLOT_ID
ENV VITE_API_URL=$VITE_API_URL
ENV VITE_ADSENSE_CLIENT_ID=$VITE_ADSENSE_CLIENT_ID
ENV VITE_ADSENSE_SLOT_ID=$VITE_ADSENSE_SLOT_ID
COPY . .
# Disable type-check if it causes issues in CI, or keep it for safety
RUN yarn build

# Production stage
FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
