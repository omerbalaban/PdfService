FROM python:latest
LABEL authors="Logo Elektronik Tic.Aş."
ENV TZ="Europe/Istanbul"
RUN apt-get update && apt-get install -y --no-install-recommends \
    libxrender1 \
    libfontconfig1 \
    libx11-dev \
    xfonts-base \
    xfonts-75dpi \
    && rm -rf /var/lib/apt/lists/*

RUN DEBIAN_FRONTEND=noninteractive TZ="Europe/Istanbul" apt-get -y install tzdata
RUN apt-get update && apt-get install -y wkhtmltopdf

WORKDIR /usr/src/app
COPY . /usr/src/app
RUN pip install --no-cache-dir -r requirements.txt
EXPOSE 8000
CMD [ "python", "./main.py" ]
