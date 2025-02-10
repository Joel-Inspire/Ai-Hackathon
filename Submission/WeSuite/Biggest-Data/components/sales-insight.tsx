import React from "react";

interface Insight {
  keyInsight: string;
  trendAnalysis: string;
  recommendedAction: string;
}

interface SalesInsightsProps {
  year: number;
  message: string;
  insights: Insight[];
}

const defaultData = {
  year: new Date().getFullYear(),
  message: "Here are the key sales insights for this year.",
  insights: [
    {
      keyInsight: "Key Insight",
      trendAnalysis: "Analyzing trend...",
      recommendedAction: "Action to take",
    },
  ],
};

const SalesInsights: React.FC<{ data?: SalesInsightsProps }> = ({
  data = defaultData,
}) => {
  if (!data) return <p>No data available.</p>;

  return (
    <div className="p-4 border rounded-lg shadow-md bg-white">
      <h2 className="text-xl font-semibold text-gray-900">
        Sales Insights for {data.year}
      </h2>
      <p className="text-gray-700 mt-2">{data.message}</p>
      <ul className="mt-4 space-y-2">
        {data.insights.map((insight, index) => (
          <li key={index} className="p-2 border rounded-md bg-gray-100">
            <div className="font-medium text-gray-900">
              <span>Key Insight: </span>
              {insight.keyInsight}
            </div>
            <div className="font-medium text-gray-900">
              <span >
                Trend Analysis:{" "}
              </span>
              {insight.trendAnalysis}
            </div>
            <div className="font-medium text-gray-900">
              <span >
                Recommended Action:{" "}
              </span>
              {insight.recommendedAction}
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
};

export default SalesInsights;
