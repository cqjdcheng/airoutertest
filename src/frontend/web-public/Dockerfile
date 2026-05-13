FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
COPY src/frontend/web-public/package.json src/frontend/web-public/package.json
RUN pnpm install --frozen-lockfile --filter cheapai-web-public...

COPY src/frontend/web-public src/frontend/web-public
ARG NEXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5050
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
WORKDIR /app/src/frontend/web-public
RUN pnpm build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
RUN corepack enable
COPY --from=build /app/package.json /app/pnpm-lock.yaml /app/pnpm-workspace.yaml /app/.npmrc ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/src/frontend/web-public ./src/frontend/web-public
EXPOSE 3000
CMD ["pnpm", "--dir", "src/frontend/web-public", "start", "-p", "3000", "-H", "0.0.0.0"]
