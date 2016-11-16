#!/usr/bin/env bash
#17/10/2016 tomas - bootstrapt by getting sources from github, cloud version

##################################################
# get a wp6 master (3.3 MB) or git clone (21 MB) #
##################################################

# echo downloading west-life-wp6
# wget --quiet https://github.com/h2020-westlife-eu/west-life-wp6/archive/master.zip
# unzip -q master.zip -d /home/vagrant
# rm master.zip

# or alternatively git clone, commented
# git clone https://github.com/h2020-westlife-eu/west-life-wp6.git
# mv west-life-wp6 west-life-wp6-master
#
# export WP6SRC=/home/vagrant/west-life-wp6-master/wp6-virtualfolder

#################################################
# or use wp6 sources from vagrant shared folder #
#################################################

export WP6SRC=/home/vagrant/work/local/wp6-virtualfolder

###########################
# launch bootstrap script #
###########################

mkdir -p /home/vagrant/bootstrap
cp -R $WP6SRC/bootstrap/* /home/vagrant/bootstrap
dos2unix /home/vagrant/bootstrap/*
chmod ugo+x /home/vagrant/bootstrap/*.sh
chown -R vagrant:vagrant /home/vagrant/bootstrap
/home/vagrant/bootstrap/bootstrap.sh