# -*- mode: ruby -*-
# vi: set ft=ruby :
# changes:
# - tomas 11/07/2016 new dev branch for lubuntu desktop (16.04) LTS - 3 years support
# - tomas 15/06/2016 changed to xenial (ubuntu 16.04).
# - tomas 20/05/2016 changed from precise64 (ubuntu 12.04) to trusty64 (ubuntu 14.04) - newer version of packages,
# - tomas 19/07/2016 changed to lubuntu 16.04 and insert_key false to prevent second vagrant up fail
#                    added start.sh provision to run always - starts background processes
# - tomas 02/08/2016 changed back to xenial (ubuntu 16.04) - desktop is installed with bootstrapdesktop.sh
# - tomas 09/08/2016 changed proxy config based on environment variables rather than manually hardcoded
# - tomas 26/08/2016 prepared cernvm box (17MB) for initial startup

#if ARGV[0] == 'up'
#  if !Vagrant.has_plugin? "vagrant-reload"
#      if Vagrant::Util::Platform.windows? then
#        system "vagrant plugin install vagrant-reload"
#        exec "vagrant up"
#      else
#        system "sudo vagrant plugin install vagrant-reload"
#        exec "vagrant up"
#      end
#  end
#end

Vagrant.configure(2) do |config|
  if ENV["http_proxy"]
    config.vm.box = "westlife-eu/wp6-cernvm-dlproxy"
  else
    config.vm.box = "westlife-eu/wp6-cernvm"
  end
  config.vm.network "forwarded_port", guest: 80, host: 8080
  config.vm.network "forwarded_port", guest: 8000, host: 8000
#  config.ssh.username = "vagrant"
#  config.ssh.password = "vagrant"
  if Vagrant.has_plugin?("vagrant-proxyconf")
    if ENV["http_proxy"]
      config.proxy.http     =  ENV["http_proxy"] #"http://wwwcache.dl.ac.uk:8080"
    end
    if ENV["https_proxy"]
      config.proxy.https    = ENV["https_proxy"] #"http://wwwcache.dl.ac.uk:8080"
    end
    if ENV["no_proxy"]
      config.proxy.no_proxy = ENV["no_proxy"] #"localhost,127.0.0.1"
    end
  end
  config.vm.provider "virtualbox" do |vb|
  #   # Display the VirtualBox GUI when booting the machine
    vb.gui = true
  #
  #   # Customize the amount of memory on the VM:
    vb.memory = "2048"
    vb.cpus = "2"
    vb.customize ["modifyvm", :id, "--vram", "16"]
  end
  config.vm.synced_folder ".", "/home/vagrant/work/local", nfs: false
  config.vm.boot_timeout = 1200
  config.vm.network "private_network", type: "dhcp", auto_config: false
  config.vm.provision "shell",  path: "bootstrap.sh"
  #bug when installing virtuoso, workaround -- reboot
  #config.vm.provision :reload
end