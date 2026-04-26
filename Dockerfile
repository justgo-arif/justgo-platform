## Dockerfile
FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

RUN apt-get update \
    && apt-get install -y curl gnupg2 \
    && curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - \
    && curl https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list \
        -o /etc/apt/sources.list.d/mssql-server-2022.list \
    && apt-get update \
    && apt-get install -y mssql-server-fts \
    && apt-get clean

USER mssql