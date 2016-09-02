These scripts and files help to create user-data context for cernvm.
- change user-data.sh script which should be executed as root during first boot
- execute makeseediso.sh which will create iso/ directory and creates an iso image

To make a vagrant box: 
- download recent cernVM 4.0 RAW disk image e.g. from http://cernvm.cern.ch/releases/testing/cvm4/ucernvm-sl7.2.7-1.cernvm.x86_64.hdd
- convert it to vmdk - e.g. qemu-img convert -O vmdk ucern.hdd ucern.vmdk
- create new virtualbox machine, disk 1 with ucern.vmdk, disk 2 as new empty disk e.g. 40GB, disk 3 existing disk of user-data.vmdk
- execute: vagrant package --base virtualmachinename
- replace the Vagrantfile inside the package.box with the Vagrantfile provided
  