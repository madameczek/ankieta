# polls-web – Angular SPA served by nginx (D18). Build context: src/polls-angular, plus deploy/ as named context
#   docker build -f deploy/polls-web.Dockerfile --build-context deploy=deploy -t polls-web src/polls-angular
# /api is routed to polls-api by the cluster route (D19), not by this nginx.

FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

# Unprivileged nginx: runs as non-root, listens on 8080
FROM nginxinc/nginx-unprivileged:stable-alpine AS runtime
COPY --from=build /src/dist/polls-angular/browser /usr/share/nginx/html
COPY --from=deploy --chmod=0644 nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
