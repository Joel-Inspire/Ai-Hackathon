<template>
  <ApplicationEdit :configuration="configuration">
    <template v-slot:additionalButtons>
      <kButton
        @click="generateTemplateClickHandler"
        class="generateTemplateButton"
        :svg-icon="sparklesIcon"
        :rounded="'full'"
      >
        {{ t("buttons.GenerateTemplate") }}
      </kButton>
    </template>
    <template v-slot:formFields>
      <ApplicationField
        columnName="templateName"
        :disabled="true"
        v-model="templateName"
      ></ApplicationField>
      <ApplicationField
        columnName="cultureName"
        v-model="selectedCultureName"
        :keyField="true"
        :validator="cultureNameValidator"
        columnControlType="DropDownList"
        :getDropDownListItems="getcultureDropDownListItems"
        @change="cultureNameChangeHandler"
      ></ApplicationField>

      <ApplicationField
        columnName="documentName"
        :validator="documentNameValidator"
      ></ApplicationField>
      <ApplicationField
        columnName="documentSubject"
        :validator="documentSubjectValidator"
      ></ApplicationField>
      <ApplicationField
        columnName="messageBody"
        column-control-type="HtmlEditor"
        :additionalProperties="{ AddToolInsertSnippet: true }"
        :updatedvalue="updatedMessageBody"
      ></ApplicationField>
    </template>
  </ApplicationEdit>

  <Popup
    :show="showGenerateTemplatePopup"
    :popup-class="'genTemplate-popup-content'"
  >
    <DocumentTemplateGenerate
      @closePopup="closeCheckoutPopupHandler"
      @acceptTemplate="acceptTemplateHandler"
      :templatename="templateName"
      :culturename="cultureName"
    />
  </Popup>
</template>
<script setup lang="ts">
import { useI18n } from "vue-i18n";
const { t } = useI18n();
import {
  CreateRequiredFieldValidator,
  CreateFieldLengthValidator,
} from "@/validators/StringValidators.ts";
import {
  DocumentTemplateCultureSpecificGet,
  DocumentTemplateCultureSpecificCreate,
  DocumentTemplateCultureSpecificUpdate,
} from "@/api/DocumentTemplateCultureSpecificApi.ts";
import { CultureGetList } from "@/api/CultureApi.ts";
import { useRoute } from "vue-router";
import { ref } from "vue";

import { Popup } from "@progress/kendo-vue-popup";
import DocumentTemplateGenerate from "./DocumentTemplateGenerate.vue";
import { Button as kButton } from "@progress/kendo-vue-buttons";
import { useLogger } from "vue-logger-plugin";

import { sparklesIcon } from "@progress/kendo-svg-icons";

let logger = useLogger();
let route = useRoute();

const selectedCultureName = ref<string>("");
const showGenerateTemplatePopup = ref(false);
const templateName = ref<string>(route.params.templateName as string);
const cultureName = ref<string>(route.params.cultureName as string);
const updatedMessageBody = ref<string>("");

if (cultureName.value == "" || cultureName.value === "null") {
  cultureName.value = "en-US";
}

let configuration = {
  GetAdditionalBreadCrumbRoutes: (route: any) => [
    { name: "DocumentTemplateList" },
    { name: "DocumentTemplateCultureSpecificList", params: route.params },
  ],
  GetFormKey: (route: any) => route.params.cultureName,
  CallApiGet: (route: any) =>
    DocumentTemplateCultureSpecificGet(
      route.params.templateName,
      route.params.cultureName,
    ),
  CallApiCreate: (dataItem: any) =>
    DocumentTemplateCultureSpecificCreate(dataItem),
  CallApiUpdate: (dataItem: any) =>
    DocumentTemplateCultureSpecificUpdate(dataItem),
  GetFormMode: (route: any) => (route.params.cultureName ? "update" : "create"),
  GetFormInitialValuesForCreateMode: (route: any) => ({
    templateName: route.params.templateName,
    cultureName: "en-US",
  }),
  GetNavigateBackRoute: (route: any) => ({
    name: "DocumentTemplateCultureSpecificList",
    params: route.params,
  }),
} as IApplicationEditConfiguration;

let cultureNameValidator = [CreateRequiredFieldValidator(t, "CultureName")];

let documentNameValidator = [
  CreateRequiredFieldValidator(t, "DocumentName"),
  CreateFieldLengthValidator(t, "DocumentName", 100),
];
let documentSubjectValidator = [
  CreateRequiredFieldValidator(t, "DocumentSubject"),
  CreateFieldLengthValidator(t, "DocumentSubject", 2000),
];

let getcultureDropDownListItems = async () => {
  let response = await CultureGetList();
  let cultureDropDownListItems = response.items
    .map((item: any) => {
      return { id: item.cultureName, text: item.cultureName };
    })
    .sort((a: { id: string; text: string }, b: { id: string; text: string }) =>
      a.text.localeCompare(b.text),
    );
  return cultureDropDownListItems;
};

function generateTemplateClickHandler(...args: any[] | unknown[]) {
  logger.debug("generateTemplateClickHandler event:", args);
  let event = args[0] as Event;
  event.preventDefault();

  showGenerateTemplatePopup.value = true;
}

function closeCheckoutPopupHandler() {
  logger.debug("closeCheckoutPopupHandler");
  showGenerateTemplatePopup.value = false;
}

function acceptTemplateHandler(generatedTemmplateHTML: string) {
  logger.debug("acceptTemplateHandler", generatedTemmplateHTML);
  updatedMessageBody.value = generatedTemmplateHTML;
}

function cultureNameChangeHandler(event: any) {
  logger.debug("cultureNameChangeHandler event:", event);
  selectedCultureName.value = event.value;
  cultureName.value = event.value;
}
</script>

<style>
.generateTemplateButton {
  background-color: #97e28b !important;
  border-color: #97e28b !important;
}

.genTemplate-popup-content {
  position: fixed;
  top: 50%;
  left: 50%;
  padding: 30px !important;
  transform: translate(-50%, -50%);
  z-index: 10004; /* Specify a stack order in case you're using a different order for other elements */
  width: 80vw !important;
}
</style>
