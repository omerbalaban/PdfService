FROM mcr.microsoft.com/dotnet/sdk:6.0
RUN groupadd -g 1001 appuser && useradd -r -u 1001 -g appuser appuser
Copy . /app
WORKDIR /app
RUN apt-get update -y
RUN apt-get upgrade -y
RUN apt-get install emacs=1:27.1+1-3.1+deb11u6 wget=1.21-1+deb11u2 unzip=6.0-26+deb11u1 -y
RUN apt-get install libpango1.0-0=1.46.2-3 -y
RUN apt-get install libc6=2.31-13+deb11u13 -y
RUN apt install -y libc6-dev=2.31-13+deb11u13
RUN apt install -y libgtk2.0-0=2.24.33-2+deb11u1
RUN apt install -y libnss3=2:3.61-1+deb11u4
RUN apt install -y libatk-bridge2.0-0=2.38.0-1
RUN apt install -y libx11-xcb1=2:1.7.2-1+deb11u2
RUN apt install -y libxcb-dri3-0=1.14-3
RUN apt install -y libgbm1=20.3.5-1
RUN apt install -y libasound2=1.2.4-1.1
# RUN apt install -y libappindicator3-1 19-25
RUN apt install -y libayatana-ido3-0.4-0=0.8.2-1
RUN apt install -y libayatana-indicator3-7=0.8.4-1+deb11u2
RUN apt install -y libdbusmenu-glib4=18.10.20180917~bzr492+repack1-2
RUN apt install -y libdbusmenu-gtk3-4=18.10.20180917~bzr492+repack1-2
RUN apt install -y libgdk-pixbuf-xlib-2.0-0=2.40.2-2
RUN apt install -y libgdk-pixbuf2.0-0=2.40.2-2
RUN apt install -y libayatana-appindicator3-1=0.5.5-2+deb11u2
RUN apt install -y libxrender1=1:0.9.10-1
RUN apt install -y libfontconfig1=2.13.1-4.2
RUN apt install -y libxshmfence1=1.3-1
RUN apt install -y libgdiplus=6.0.4+dfsg-2
RUN ln -s /usr/lib/libgdiplus.so /lib/x86_64-linux-gnu/libgdiplus.so
RUN apt-get install -y --no-install-recommends zlib1g=1:1.2.11.dfsg-2+deb11u2 fontconfig=2.13.1-4.2 libfreetype6=2.10.4+dfsg-1+deb11u2 libx11-6=2:1.7.2-1+deb11u2 libxext6=2:1.3.3-1.1 libxrender1=1:0.9.10-1 wget=1.21-1+deb11u2 gdebi=0.9.5.7+nmu5
RUN wget https://github.com/wkhtmltopdf/wkhtmltopdf/releases/download/0.12.5/wkhtmltox_0.12.5-1.stretch_amd64.deb
RUN gdebi --n wkhtmltox_0.12.5-1.stretch_amd64.deb
RUN apt install libssl1.1=1.1.1w-0+deb11u4
RUN ln -s /usr/local/lib/libwkhtmltox.so /usr/lib/libwkhtmltox.so
RUN chmod 777 .
# Widevine CDM (Digital Rights Management) modülü indirilip açılıyor.
# Bu modül, genellikle Netflix gibi platformlardaki DRM korumalı içerikleri oynatmak için kullanılır.
# IronPDF'in çalışması için bu satırlara ihtiyaç yoktur.
# RUN wget https://dl.google.com/widevine-cdm/1.4.9.1088-linux-x64.zip
# RUN unzip -o 1.4.9.1088-linux-x64.zip
# RUN mkdir /usr/lib/chromium
# RUN mv ./libwidevinecdm.so /usr/lib/chromium
# RUN chmod 644 /usr/lib/chromium/libwidevinecdm.so
RUN cp -r ./Fonts/* /usr/share/fonts/
RUN chown -R appuser:appuser /app
USER appuser
ENTRYPOINT ["dotnet", "eLogo.PdfService.Api.dll"]
EXPOSE 8100
