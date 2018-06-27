//Configure Bluebird Promises not to throw warnings about aurelia empty promises
Promise.config({
  warnings: {
    wForgottenReturn: false
  }
});

export function configure(aurelia) {
  aurelia.use
    .standardConfiguration()
    .plugin('aurelia-dialog')
    .feature('resources')
    .developmentLogging();
  aurelia.start().then(() => aurelia.setRoot());
}
