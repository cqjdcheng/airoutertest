FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
COPY src/frontend/web-admin/package.json src/frontend/web-admin/package.json
RUN pnpm install --frozen-lockfile --filter cheapai-web-admin...

COPY src/frontend/web-admin src/frontend/web-admin
ARG UMI_APP_API_BASE_URL=http://127.0.0.1:5050
ENV UMI_APP_API_BASE_URL=$UMI_APP_API_BASE_URL
RUN pnpm --dir src/frontend/web-admin build

FROM nginx:1.27-alpine AS runtime
COPY src/frontend/web-admin/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/src/frontend/web-admin/dist /usr/share/nginx/html
EXPOSE 80
