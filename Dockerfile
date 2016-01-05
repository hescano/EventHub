FROM debian:wheezy

#Originall MAINTAINER Jo Shields <jo.shields@xamarin.com>
#based on dockerfile by Michael Friis <friism@gmail.com>

RUN apt-key adv --keyserver pgp.mit.edu --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
	&& echo "deb http://download.mono-project.com/repo/debian wheezy/snapshots/4.0.0 main" > /etc/apt/sources.list.d/mono-xamarin.list

RUN apt-get update \
	&& apt-get install -y mono-xsp4 mono-xsp4-base \
	&& rm -rf /var/lib/apt/lists/*

RUN mkdir -p /var/www/html/test

ADD test/ /var/www/html/test

RUN cp /var/www/html/test/EventHub/EventHub/RabbitMQ.Client.dll /usr/lib/mono/4.0

RUN xbuild /var/www/html/test/EventHub/EventHub.sln

EXPOSE 5000

WORKDIR /var/www/html/test/EventHub/EventHub/bin/Debug

CMD mono /var/www/html/test/EventHub/EventHub/bin/Debug/EventHub.exe
