# README #

API using Spring AI to generate images.

### What is this repository for? ###

* AI image generation based on a category and prompt
* Initial use-case is DB Commerce catalog image generation


### Local development ###

* Environment variables - in cloud foundry provided by the app manifests (manifest-dev.yml, 
  manifest-prod.yml, application-devcf.yml) and
  open-ai-service credentials:
  * api.base-domain=.run.development.cf.demandbridge.io;
  * CONFIG_SERVER_ADDRESS=http://config.run.development.cf.demandbridge.io;
  * spring.application.name=db-image-generator-[you];
  * spring.profiles.active=devcf;
  * vcap.services.openai-service.credentials.OPENAI_API_KEY=[omitted];

