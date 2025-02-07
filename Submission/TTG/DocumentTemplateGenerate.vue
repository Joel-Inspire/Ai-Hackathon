<template>
  <div class="document-template-generate">
    <template v-if="isGeneratingTemplate">
      <div class="row">
        <div class="col-12 text-center position-absolute top-50">
          <KLoader :size="'large'" />
          <div>{{ t("generateTemplate.GeneratingTemplate") }}</div>
        </div>
      </div>
    </template>
    <div class="row">
      <div class="col-11">
        {{ t("generateTemplate.GenerateTemplateHeader") }}
      </div>
      <div class="col-1 closePopup">
        <KButton
          :fill-mode="'clear'"
          @click="closeCheckoutPopupHandler"
          class="popupCloseButton"
          >&#x2715;</KButton
        >
      </div>
    </div>
    <div class="row mt-3">
      <div class="col-1 align-self-center">
        {{ t("generateTemplate.AIInstructions") }}
      </div>
      <div class="col-9">
        <kInput v-model="aiInstructions" />
      </div>
      <div class="col-2 text-center">
        <kButton
          @click="generateTemplateClickHandler"
          :svg-icon="sparklesIcon"
          :rounded="'full'"
          :theme-color="'primary'"
        >
          {{ generateButtonText }}
        </kButton>
      </div>
    </div>
    <div class="row mt-3">
      <KExpansionPanel
        :title="t('generateTemplate.Tokens')"
        :expanded="tokenListExpanded"
        @action="tokenListPanelActionHandler"
      >
        <KExpansionPanelContent v-if="tokenListExpanded">
          <div class="row">
            <div class="col-12">
              {{ t("generateTemplate.GenerateUseTokens") }}
            </div>
            <div
              v-for="item in tokenList"
              :key="item"
              class="col-4"
            >
              <kCheckbox
                class="tokenCheckBox"
                :value="item.id"
                :default-checked="selectedTokens.includes(item.text)"
                :label="item.text"
                @change="tokenClickHandler($event, item.text)"
              >
              </kCheckbox>
            </div>
          </div>
        </KExpansionPanelContent>
      </KExpansionPanel>
    </div>
    <div class="row mt-3">
      <KExpansionPanel
        :title="t('generateTemplate.GeneratedTemplate')"
        :expanded="generatedTemplateExpanded"
        @action="generatedTemplatePanelActionHandler"
      >
        <KExpansionPanelContent v-if="generatedTemplateExpanded">
          <div class="row">
            <div
              class="col-12 templateHTMLDiv"
              v-html="generatedTemplateHTML"
            ></div>
          </div>
        </KExpansionPanelContent>
      </KExpansionPanel>
    </div>
    <div
      class="row mt-3"
      v-if="generatedTemplateHTML && generatedTemplateHTML.length > 0"
    >
      <div class="col-12 text-center">
        <kButton
          @click="acceptGeneratedPopupHandler"
          class="acceptButton"
        >
          {{ t("generateTemplate.AcceptButton") }}
        </kButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Button as kButton } from "@progress/kendo-vue-buttons";
import {
  Input as kInput,
  Checkbox as kCheckbox,
} from "@progress/kendo-vue-inputs";
import { Loader as KLoader } from "@progress/kendo-vue-indicators";
import { useLogger } from "vue-logger-plugin";
import { ref, onMounted } from "vue";
import {
  DocumentTemplateTokenGetList,
  DocumentTemplateGenerateAI,
} from "@/api/DocumentTemplateApi.ts";
import { Button as KButton } from "@progress/kendo-vue-buttons";
import {
  ExpansionPanel as KExpansionPanel,
  ExpansionPanelContent as KExpansionPanelContent,
} from "@progress/kendo-vue-layout";
import Swal from "sweetalert2";
import { sparklesIcon } from "@progress/kendo-svg-icons";

const tokenListExpanded = ref(true);
const generatedTemplateExpanded = ref(false);
const isGeneratingTemplate = ref(false);
const logger = useLogger();

const { t } = useI18n();
const aiInstructions = ref<string>("Generate an email template");
const originalAIInstructions = aiInstructions.value;
const tokenList = ref<any[]>([]);
const selectedTokens = ref<string[]>([]);
const emit = defineEmits(["closePopup", "acceptTemplate"]);
const generatedTemplateHTML = ref<string>("");
const generateButtonText = ref(t("generateTemplate.GenerateButton"));

let props = defineProps({
  templatename: {
    type: String,
    required: true,
  },
  culturename: {
    type: String,
    required: true,
  },
});

onMounted(async () => {
  if (props.templatename) {
    let response = await DocumentTemplateTokenGetList(props.templatename);
    logger.debug("response:", response);
    tokenList.value = response.items.sort().map((item: any) => {
      return { id: "{{ " + item + " }}", text: item };
    });

    const tokenCount = tokenList.value.length;
    const size = Math.ceil(tokenCount / 3);
    const tokenListBy3 = [];

    for (let i = 0; i < 3; i++) {
      tokenListBy3.push(tokenList.value.slice(i * size, (i + 1) * size));
    }

    tokenList.value = [];
    for (let i = 0; i < size; i++) {
      tokenListBy3[0][i] && tokenList.value.push(tokenListBy3[0][i]);
      tokenListBy3[1][i] && tokenList.value.push(tokenListBy3[1][i]);
      tokenListBy3[2][i] && tokenList.value.push(tokenListBy3[2][i]);
    }
    logger.debug("tokenCount:", tokenCount, tokenList.value.length);
  }
});

async function generateTemplateClickHandler(...args: any[] | unknown[]) {
  logger.debug("generateTemplateClickHandler event:", args);
  if (originalAIInstructions == aiInstructions.value) {
    logger.debug("AI Instructions not changed");
    await Swal.fire({
      text: t("generateTemplate.InstructionsNotCustomized"),
    });
    return;
  }

  tokenListExpanded.value = false;
  generateButtonText.value = t("generateTemplate.RegenerateButton");

  isGeneratingTemplate.value = true;
  DocumentTemplateGenerateAI({
    templateName: props.templatename,
    cultureName: props.culturename,
    userPrompts: [aiInstructions.value],
    tokens: selectedTokens.value,
  }).then((response) => {
    isGeneratingTemplate.value = false;
    logger.debug("response:", response);
    generatedTemplateExpanded.value = true;
    generatedTemplateHTML.value = response.item;
    logger.debug("generatedTemplateHTML:", generatedTemplateHTML.value);
    logger.debug("response.item:", response.item);

  });
}

function tokenListPanelActionHandler(event: any) {
  logger.debug("tokenListPanelActionHandler event:", event);
  tokenListExpanded.value = !tokenListExpanded.value;
}
function generatedTemplatePanelActionHandler(event: any) {
  logger.debug("generatedTemplatePanelActionHandler event:", event);
  generatedTemplateExpanded.value = !generatedTemplateExpanded.value;
}

function tokenClickHandler(event: any, selectedItem: string) {
  logger.debug("tokenClickHandler event:", event);

  if (event.value) {
    selectedTokens.value.push(selectedItem);
  } else {
    selectedTokens.value = selectedTokens.value.filter(
      (x) => x !== selectedItem,
    );
  }
}

function closeCheckoutPopupHandler() {
  logger.debug("closeCheckoutPopupHandler");
  emit("closePopup");
}

function acceptGeneratedPopupHandler() {
  logger.debug("acceptGeneratedPopupHandler");
  emit("closePopup");
  emit("acceptTemplate", generatedTemplateHTML.value);
}
</script>

<style>
.document-template-generate {
  .k-expander  {
    padding: 0px;
  }
  .k-expander-content{
    padding: 2px 5px 5px 10px; 
  }
  .k-expander .k-expander-header {
    padding-block: 0px;
    padding-inline: 0px;
  }
  .k-expander-title
  {
    text-transform: none;
    padding: 10px;
  }
  .tokenCheckBox input {
    margin-right: 10px;
  }

  .popupCloseButton {
    color: black;
    transform: scale(1.5);
    position: fixed !important;
    top: 0px;
    right: 0px;
  }

  .templateHTMLDiv {
    height: 300px;
    overflow: scroll;
  }
}
</style>
