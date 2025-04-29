# API de Geração de PDF a partir de HTML (.NET 8 + WkHtmlToPdf + Docker)

## Descrição

Este projeto implementa uma API REST utilizando .NET 8 Minimal API para converter conteúdo HTML em arquivos PDF. A conversão é realizada utilizando a biblioteca [WkHtmlToPdf-DotNet](https://github.com/HakanL/WkHtmlToPdf-DotNet), que é um wrapper .NET para a ferramenta wkhtmltopdf, fornecendo uma solução eficiente e leve para a geração de PDFs a partir de HTML.

A aplicação é projetada para ser executada dentro de um container Docker, que inclui todas as dependências necessárias (runtime .NET, wkhtmltopdf, bibliotecas de sistema).

## Tecnologias Utilizadas

* **.NET 8:** Plataforma de desenvolvimento.
* **ASP.NET Core Minimal API:** Framework para construção da API REST.
* **WkHtmlToPdf-DotNet:** Biblioteca wrapper .NET para wkhtmltopdf.
* **wkhtmltopdf:** Ferramenta de linha de comando para renderizar HTML em PDF.
* **Docker:** Plataforma de containerização para empacotamento e distribuição da aplicação e suas dependências.

## Funcionalidades (Endpoints)

A API expõe os seguintes endpoints (por padrão na porta 8080 dentro do container):

1. **`POST /generate-pdf-from-body`**
   * **Entrada:** Conteúdo HTML bruto (`text/html`) enviado no corpo da requisição.
   * **Saída:** Arquivo PDF (`application/pdf`).
   * **Observação:** CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas (`https://...`) ou estar embutidos diretamente no HTML (tags `<style>`, `<script>`, imagens base64) para serem renderizados corretamente. Links relativos não funcionarão.

2. **`POST /generate-pdf-from-url`**
   * **Entrada:** JSON (`application/json`) no corpo da requisição com a estrutura: `{ "url": "SUA_URL_AQUI" }`.
   * **Saída:** Arquivo PDF (`application/pdf`).
   * **Observação:** A API navegará até a URL fornecida, carregando seus recursos (CSS, JS, imagens, fontes), antes de gerar o PDF.

3. **`POST /generate-pdf-from-file`**
   * **Entrada:** Upload de arquivo (`multipart/form-data`) com o campo `file` contendo o arquivo HTML.
   * **Saída:** Arquivo PDF (`application/pdf`) com o nome baseado no arquivo original.
   * **Observação:** Similar ao endpoint `/generate-pdf-from-body`, CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas ou estar embutidos. Links relativos não funcionarão.

## Como Usar (Docker)

1. **Construir a Imagem Docker:**
   Navegue até o diretório raiz do projeto (onde está o `Dockerfile`) e execute:
   ```bash
   docker build -t api-pdf-wkhtmltopdf:1.0 .
   ```
   (Substitua `api-pdf-wkhtmltopdf:1.0` pelo nome e tag desejados).

2. **Executar o Container:**
   ```bash
   docker run -d -p 8081:8080 --name container-pdf api-pdf-wkhtmltopdf:1.0
   ```
   * `-d`: Executa em modo detached (background).
   * `-p 8081:8080`: Mapeia a porta `8081` da sua máquina host para a porta `8080` dentro do container (onde a API escuta por padrão). Você pode alterar a porta do host (`8081`) se necessário.
   * `--name container-pdf`: Dá um nome ao container para facilitar o gerenciamento.
   * `api-pdf-wkhtmltopdf:1.0`: O nome da imagem que você construiu.

3. **Acessar a API:**
   * **Swagger UI:** Abra seu navegador e acesse `http://localhost:8081/`. Você será redirecionado para a interface do Swagger para explorar e testar os endpoints.
   * **Endpoints (Exemplos com `curl`):**

      * **Via Corpo HTML (usando um arquivo `teste.html`):**
         ```bash
         curl -X POST "http://localhost:8081/generate-pdf-from-body" \
              -H "Content-Type: text/html" \
              --data-binary "@teste.html" \
              -o output_body.pdf --fail
         ```

      * **Via URL:**
         ```bash
         curl -X POST "http://localhost:8081/generate-pdf-from-url" \
              -H "Content-Type: application/json" \
              -d "{\"url\": \"https://example.com\"}" \
              -o output_url.pdf --fail
         ```

      * **Via Upload de Arquivo (usando um arquivo `teste.html`):**
         ```bash
         curl -X POST "http://localhost:8081/generate-pdf-from-file" \
              -F "file=@teste.html" \
              -o output_file.pdf --fail
         ```

## Configuração do Dockerfile

O Dockerfile para esta implementação precisa instalar o wkhtmltopdf e suas dependências. Exemplo de Dockerfile:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos do projeto
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar wkhtmltopdf e dependências
RUN apt-get update && apt-get install -y \
    libssl-dev \
    libxrender-dev \
    libx11-dev \
    libxext-dev \
    libfontconfig1-dev \
    libfreetype6-dev \
    fontconfig \
    xfonts-base \
    xfonts-75dpi \
    fonts-liberation \
    libjpeg-dev \
    && apt-get install -y wget \
    && wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && dpkg -i wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && rm wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && apt-get -f install \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Copiar arquivos da aplicação
COPY --from=build /app/publish .

# Configurar porta e entry point
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "pdf-conversor-api.dll"]
```

## Prós e Contras da Abordagem WkHtmlToPdf

### Prós

* **Tamanho Reduzido:** A imagem Docker com wkhtmltopdf é significativamente menor (centenas de MB) comparada à solução com Puppeteer/Chromium (1GB+).
* **Menor Consumo de Recursos:** wkhtmltopdf usa menos CPU e memória que uma instância de Chromium headless.
* **Velocidade:** Geralmente mais rápido para renderizar documentos simples e médios.
* **Facilidade de Integração:** O wrapper WkHtmlToPdf-DotNet fornece uma API simples e intuitiva.
* **Portabilidade:** A ferramenta wkhtmltopdf está disponível para várias plataformas e pode ser facilmente instalada em containers Docker.

### Contras

* **Suporte a JavaScript Limitado:** wkhtmltopdf utiliza uma versão antiga do QtWebKit, com suporte limitado a JavaScript moderno. Pode não renderizar corretamente páginas que dependem fortemente de JS avançado.
* **Renderização CSS Menos Fiel:** Alguns recursos de CSS3 mais recentes podem não ser suportados ou renderizados corretamente.
* **Menos Flexibilidade:** Comparado ao Puppeteer, oferece menos opções para interagir com a página antes da geração do PDF.
* **Dependências Externas:** Requer a instalação do binário wkhtmltopdf e suas dependências no sistema operacional.

## Considerações Adicionais

* **Fontes:** Para garantir a correta renderização de fontes, o Dockerfile inclui o pacote `fonts-liberation`. Se precisar de fontes adicionais, instale-as via `apt-get` no Dockerfile.
* **Configurações Avançadas:** A biblioteca WkHtmlToPdf-DotNet oferece várias opções de configuração para personalizar a geração de PDF, como tamanho de papel, margens, cabeçalhos/rodapés, etc.
* **Alternativas:** Para casos com requisitos de fidelidade visual muito alta e suporte completo a JavaScript moderno, a abordagem com Puppeteer pode ser mais adequada, apesar do maior consumo de recursos.
* **Performance em Alta Carga:** A implementação atual inicializa o conversor como singleton via DI, o que é eficiente para uso em ambiente de produção com múltiplas requisições.