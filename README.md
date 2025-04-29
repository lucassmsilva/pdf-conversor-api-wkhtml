# API de Gera��o de PDF a partir de HTML (.NET 8 + WkHtmlToPdf + Docker)

## Descri��o

Este projeto implementa uma API REST utilizando .NET 8 Minimal API para converter conte�do HTML em arquivos PDF. A convers�o � realizada utilizando a biblioteca [WkHtmlToPdf-DotNet](https://github.com/HakanL/WkHtmlToPdf-DotNet), que � um wrapper .NET para a ferramenta wkhtmltopdf, fornecendo uma solu��o eficiente e leve para a gera��o de PDFs a partir de HTML.

A aplica��o � projetada para ser executada dentro de um container Docker, que inclui todas as depend�ncias necess�rias (runtime .NET, wkhtmltopdf, bibliotecas de sistema).

## Tecnologias Utilizadas

* **.NET 8:** Plataforma de desenvolvimento.
* **ASP.NET Core Minimal API:** Framework para constru��o da API REST.
* **WkHtmlToPdf-DotNet:** Biblioteca wrapper .NET para wkhtmltopdf.
* **wkhtmltopdf:** Ferramenta de linha de comando para renderizar HTML em PDF.
* **Docker:** Plataforma de containeriza��o para empacotamento e distribui��o da aplica��o e suas depend�ncias.

## Funcionalidades (Endpoints)

A API exp�e os seguintes endpoints (por padr�o na porta 8080 dentro do container):

1. **`POST /generate-pdf-from-body`**
   * **Entrada:** Conte�do HTML bruto (`text/html`) enviado no corpo da requisi��o.
   * **Sa�da:** Arquivo PDF (`application/pdf`).
   * **Observa��o:** CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas (`https://...`) ou estar embutidos diretamente no HTML (tags `<style>`, `<script>`, imagens base64) para serem renderizados corretamente. Links relativos n�o funcionar�o.

2. **`POST /generate-pdf-from-url`**
   * **Entrada:** JSON (`application/json`) no corpo da requisi��o com a estrutura: `{ "url": "SUA_URL_AQUI" }`.
   * **Sa�da:** Arquivo PDF (`application/pdf`).
   * **Observa��o:** A API navegar� at� a URL fornecida, carregando seus recursos (CSS, JS, imagens, fontes), antes de gerar o PDF.

3. **`POST /generate-pdf-from-file`**
   * **Entrada:** Upload de arquivo (`multipart/form-data`) com o campo `file` contendo o arquivo HTML.
   * **Sa�da:** Arquivo PDF (`application/pdf`) com o nome baseado no arquivo original.
   * **Observa��o:** Similar ao endpoint `/generate-pdf-from-body`, CSS, JavaScript e imagens referenciados neste HTML **devem** usar URLs absolutas ou estar embutidos. Links relativos n�o funcionar�o.

## Como Usar (Docker)

1. **Construir a Imagem Docker:**
   Navegue at� o diret�rio raiz do projeto (onde est� o `Dockerfile`) e execute:
   ```bash
   docker build -t api-pdf-wkhtmltopdf:1.0 .
   ```
   (Substitua `api-pdf-wkhtmltopdf:1.0` pelo nome e tag desejados).

2. **Executar o Container:**
   ```bash
   docker run -d -p 8081:8080 --name container-pdf api-pdf-wkhtmltopdf:1.0
   ```
   * `-d`: Executa em modo detached (background).
   * `-p 8081:8080`: Mapeia a porta `8081` da sua m�quina host para a porta `8080` dentro do container (onde a API escuta por padr�o). Voc� pode alterar a porta do host (`8081`) se necess�rio.
   * `--name container-pdf`: D� um nome ao container para facilitar o gerenciamento.
   * `api-pdf-wkhtmltopdf:1.0`: O nome da imagem que voc� construiu.

3. **Acessar a API:**
   * **Swagger UI:** Abra seu navegador e acesse `http://localhost:8081/`. Voc� ser� redirecionado para a interface do Swagger para explorar e testar os endpoints.
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

## Configura��o do Dockerfile

O Dockerfile para esta implementa��o precisa instalar o wkhtmltopdf e suas depend�ncias. Exemplo de Dockerfile:

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

# Instalar wkhtmltopdf e depend�ncias
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

# Copiar arquivos da aplica��o
COPY --from=build /app/publish .

# Configurar porta e entry point
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "pdf-conversor-api.dll"]
```

## Pr�s e Contras da Abordagem WkHtmlToPdf

### Pr�s

* **Tamanho Reduzido:** A imagem Docker com wkhtmltopdf � significativamente menor (centenas de MB) comparada � solu��o com Puppeteer/Chromium (1GB+).
* **Menor Consumo de Recursos:** wkhtmltopdf usa menos CPU e mem�ria que uma inst�ncia de Chromium headless.
* **Velocidade:** Geralmente mais r�pido para renderizar documentos simples e m�dios.
* **Facilidade de Integra��o:** O wrapper WkHtmlToPdf-DotNet fornece uma API simples e intuitiva.
* **Portabilidade:** A ferramenta wkhtmltopdf est� dispon�vel para v�rias plataformas e pode ser facilmente instalada em containers Docker.

### Contras

* **Suporte a JavaScript Limitado:** wkhtmltopdf utiliza uma vers�o antiga do QtWebKit, com suporte limitado a JavaScript moderno. Pode n�o renderizar corretamente p�ginas que dependem fortemente de JS avan�ado.
* **Renderiza��o CSS Menos Fiel:** Alguns recursos de CSS3 mais recentes podem n�o ser suportados ou renderizados corretamente.
* **Menos Flexibilidade:** Comparado ao Puppeteer, oferece menos op��es para interagir com a p�gina antes da gera��o do PDF.
* **Depend�ncias Externas:** Requer a instala��o do bin�rio wkhtmltopdf e suas depend�ncias no sistema operacional.

## Considera��es Adicionais

* **Fontes:** Para garantir a correta renderiza��o de fontes, o Dockerfile inclui o pacote `fonts-liberation`. Se precisar de fontes adicionais, instale-as via `apt-get` no Dockerfile.
* **Configura��es Avan�adas:** A biblioteca WkHtmlToPdf-DotNet oferece v�rias op��es de configura��o para personalizar a gera��o de PDF, como tamanho de papel, margens, cabe�alhos/rodap�s, etc.
* **Alternativas:** Para casos com requisitos de fidelidade visual muito alta e suporte completo a JavaScript moderno, a abordagem com Puppeteer pode ser mais adequada, apesar do maior consumo de recursos.
* **Performance em Alta Carga:** A implementa��o atual inicializa o conversor como singleton via DI, o que � eficiente para uso em ambiente de produ��o com m�ltiplas requisi��es.