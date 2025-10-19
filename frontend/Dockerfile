# Use Node.js 24 Alpine as base image
FROM node:24-alpine

# Set working directory
WORKDIR /app

# Copy package files first (for better Docker layer caching)
COPY package.json yarn.lock ./

# Install dependencies
RUN yarn install --frozen-lockfile

# Copy source code
COPY . .

# Expose port 3000
EXPOSE 3000

# Start the development server with hot-reload
CMD ["yarn", "dev", "--host", "0.0.0.0", "--port", "3000"]
