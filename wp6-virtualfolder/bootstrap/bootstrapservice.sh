#!/usr/bin/env bash

# 27.06.2016 mono from distribution, nuget install from distribution
# 28.10.2016 mono from cvmfs
# 17.10.2016 replaced postgresql by sqlite3
# 10.10.2016 moved mono 4.5 build to cvmfs
# 02.06.2016 replaced mysql by postgres

# install nuget
if hash mono 2>/dev/null; then
  echo using preinstaled mono
else
  yum -y install mono-devel
fi
if hash nuget 2>/dev/null; then
  echo using preinstaled nuget
else
  yum -y install nuget  
fi
rm -rf /opt/virtualfolder/MetadataService
# fix http://stackoverflow.com/questions/15181888/
for i in {1..3}; do
    echo attemp $i
    if [ ! -f /opt/virtualfolder/MetadataService/MetadataService.exe ]
    then
       echo Building MetadataService
	  #clean from previous try
	  rm -rf /opt/virtualfolder/MetadataService
	  rm -rf /opt/virtualfolder/src
	  # build metadataservice
	  # cp -R $WP6SRC/src /opt/virtualfolder

      cert-sync /etc/pki/tls/certs/ca-bundle.crt
	  $WP6SRC/scripts/timeout3.sh -t 90 xbuild $WP6SRC/src/VFMetadata/VFMetadata.proj
	  mkdir /opt/virtualfolder/MetadataService
	  cp $WP6SRC/src/VFMetadata/MetadataService/bin/Release/* /opt/virtualfolder/MetadataService
	  cp $WP6SRC/src/VFMetadata/webdavhash2path/bin/Release/webdavhash2path.exe /opt/virtualfolder/MetadataService
	  cp $WP6SRC/src/VFMetadata/webdavhash2path/webdavhash2path /opt/virtualfolder/MetadataService
    fi
done
mkdir -p /var/log/westlife
chmod -R go+wx /var/log/westlife

#generate random key
if [ -f /etc/westlife/metadata.key ]
then
   source /etc/westlife/metadata.key
   export VF_STORAGE_PKEY
else
   export VF_STORAGE_PKEY=`openssl rand -base64 32`
   echo VF_STORAGE_PKEY=$VF_STORAGE_PKEY > /etc/westlife/metadata.key
   chmod 700 /etc/westlife
   chmod 600 /etc/westlife/metadata.key
fi
