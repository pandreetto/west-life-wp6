#!/usr/bin/env bash
# this script prepares web server, opens ports on firewall
# 02.06.2016 tomas - added WEBDAV & tiddlywiki for virtual folder documentation, probably more CMS should be supported
# 17.06.2016 tomas - added noninteractive for davfs2 in ubuntu1604
# 16.11.2016 tomas - added permission +x for SSI support

# enable firewall
#ufw allow 22
#ufw allow 80
#ufw enable

#yum -y install firewalld
#sudo systemctl start firewalld
#firewall-cmd --zone=public --add-port=80/tcp --permanent
#firewall-cmd --zone=public --add-port=22/tcp --permanent
#firewall-cmd --zone=public --add-port=8890/tcp --permanent
#firewall-cmd --zone=public --add-port=1111/tcp --permanent
# could firewall-cmd --zone=public --add-port=80/tcp --permanent
#firewall-cmd --reload

# prepare and restart apache, rewrite configuration

#one of the configuration is syslog - need to restart
service rsyslog restart

chown -R apache:apache /var/www/html
chmod -R 644 /var/www/html
find /var/www/html -type d -exec chmod ugo+rx {} \;

##add +x permission on all html files which has include directive
# x-bit hack no longer needed
#chmod ugo+x `grep -rl '/var/www/html' -e "<\!--\#include"`

yum -y install epel-release
yum-config-manager --save --setopt=epel/x86_64/metalink.skip_if_unavailable=true
yum repolist
yum -y install davfs2 --skip-broken 
yum -y install mod_proxy_html --skip-broken 
yum -y install mod_ssl --skip-broken 
yum -y install dos2unix --skip-broken

systemctl start httpd
systemctl enable httpd

# share work directory via webdav - may be used to directly pass and process data
mkdir /srv/virtualfolder
chmod ugo+rwx /srv/virtualfolder
#add permission to allow browse webdav content in /srv/virtualfolder
chmod go+rx /home/vagrant
# workaround issue #6 store some config 
mkdir /etc/westlife

chown apache:apache /srv/virtualfolder
#adding vagrant and apache into davfs2 group
usermod -a -G davfs2 vagrant
usermod -a -G davfs2 apache
# set the default group of user vagrant to davfs2, to be able to mount
usermod -g davfs2 vagrant

mkdir -p /opt/virtualfolder
ln -s $WP6SRC/scripts /opt/virtualfolder/scripts
ln -s $WP6SRC/www/dist /opt/virtualfolder/www
#no need to ln - editor is coppied into dist folder
#ln -s $WP6SRC/prov-n-editor /opt/virtualfolder/www/editor
ln -s $WP6SRC/singlevre /opt/virtualfolder/singlevre
# chown -R apache:apache /opt/virtualfolder/www
chmod -R 755 /opt/virtualfolder/www

dos2unix /opt/virtualfolder/scripts/*
chmod ugo+x /opt/virtualfolder/scripts/*

chown root:root /opt/virtualfolder/scripts/mountb2drop.sh
chmod 4755 /opt/virtualfolder/scripts/mountb2drop.sh
if  grep -q MOUNTB2 /etc/sudoers; then
  echo sudoers already provisioned
else
  cat /opt/virtualfolder/scripts/sudoers >>/etc/sudoers
  #chmod 0440 /etc/sudoers
fi

# set proxy for davfs - as it seems not taking the http_proxy environment variable

if [ -z "$http_proxy" ]; then echo "proxy is not set"; else
  echo "proxy is set to '$http_proxy'"
  # set proxy for redirecting the webdav traffic
  # TODO interprets


  #strip http:// from the variable
  davs_http_proxy=${http_proxy:7}
  echo "proxy $davs_http_proxy" >> /etc/davfs2/davfs2.conf
  echo "ask_auth    0" >> /etc/davfs2/davfs2.conf
  proxyhostport=${http_proxy#*http://}
  proxyport=${proxyhostport#*:}
  proxyhost=${proxyhostport%:*}

  sed -i -e "s/\#ProxyRemoteMatch.*$/ProxyRemoteMatch https:\/\/b2drop.eudat.eu\* http:\/\/${proxyhostport}/g" 000-default.conf

  #echo "writing to configuration proxy setting, proxyhost: $proxyhost  proxyport: $proxyport"
  #echo "\$conf['proxy']['host'] = $proxyhost;" >> /var/www/html/dokuwiki/conf/local.php
  #echo "\$conf['proxy']['port'] = $proxyport;" >> /var/www/html/dokuwiki/conf/local.php
fi
