#!/usr/bin/env bash
# 24.05.2016 tomas - changed directory structure, all mounts will be subdir of 'work', comment owncloudcmd
whoami 1>&2
#create directory if not created
mkdir -p /home/vagrant/work/b2drop
chown apache /home/vagrant/work
#mount work folder via webdav to b2drop
umount /home/vagrant/work/b2drop
#kill all previous b2dropsync and xiacont
#killall /bin/sh

#move secrets from temporary files and mount
mv /tmp/secrets /etc/davfs2/secrets
chown root:root /etc/davfs2/secrets
chmod 600 /etc/davfs2/secrets
chmod ugo+rx /var/log/httpd
mount.davfs https://b2drop.eudat.eu/remote.php/webdav /home/vagrant/work/b2drop
#configure reverse proxy for webdav in apache
#encode base64 authentication string and pass it to header where "Basic ...." is already been placed
if [ -e /tmp/secrets2 ] 
  then
  AUTH="$(base64 /tmp/secrets2)"
  sed -i -e "s/\"Basic [^\"]*/\"Basic ${AUTH}/g" /etc/httpd/conf.d/000-default.conf
  service httpd restart
  rm /tmp/secrets2
fi
#chown -R www-data:www-data /home/vagrant/work
#start owncloud synchronization of local copy
#sudo -u vagrant nohup /home/vagrant/scripts/b2dropsync.sh > /home/vagrant/logs/b2drop.log 2>&1 &
#sudo -u vagrant nohup /home/vagrant/scripts/xiacont.sh > /home/vagrant/logs/xia.log 2>&1 &
